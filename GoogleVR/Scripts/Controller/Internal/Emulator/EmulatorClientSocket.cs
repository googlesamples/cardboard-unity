// Copyright 2016 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissioßns and
// limitations under the License.

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

using proto;

/// @cond
namespace Gvr.Internal {
  class EmulatorClientSocket : MonoBehaviour {
    private static readonly int kPhoneEventPort = 7003;
    private const int kSocketReadTimeoutMillis = 5000;

    // Minimum interval, in seconds, between attempts to reconnect the socket.
    private const float kMinReconnectInterval = 1f;

    private TcpClient phoneMirroringSocket;

    private Thread phoneEventThread;
    //private TcpClient phoneEventSocket;
    //private NetworkStream phoneEventStream;

    private volatile bool shouldStop = false;

    // Flag used to limit connection state logging to initial failure and successful reconnects.
    private volatile bool lastConnectionAttemptWasSuccessful = true;

    private EmulatorManager phoneRemote;
    public bool connected { get; private set; }

    public void Init(EmulatorManager remote) {
      phoneRemote = remote;

      if (EmulatorConfig.Instance.PHONE_EVENT_MODE != EmulatorConfig.Mode.OFF) {
        phoneEventThread = new Thread(phoneEventSocketLoop);
        phoneEventThread.Start();
      }
    }

    private void phoneEventSocketLoop() {
      while (!shouldStop) {
        long lastConnectionAttemptTime = DateTime.Now.Ticks;
        try {
          phoneConnect();
        } catch(Exception e) {
          if (lastConnectionAttemptWasSuccessful) {
            Debug.LogWarningFormat("{0}\n{1}", e.Message, e.StackTrace);
            // Suppress additional failures until we have successfully reconnected.
            lastConnectionAttemptWasSuccessful = false;
          }
        }

        // Wait a while in order to enforce the minimum time between connection attempts.
        TimeSpan elapsed = new TimeSpan(DateTime.Now.Ticks - lastConnectionAttemptTime);
        float toWait = kMinReconnectInterval - (float) elapsed.TotalSeconds;
        if (toWait > 0) {
          Thread.Sleep((int) (toWait * 1000));
        }
      }
    }

    private void phoneConnect() {
      string addr = EmulatorConfig.Instance.PHONE_EVENT_MODE == EmulatorConfig.Mode.USB
        ? EmulatorConfig.USB_SERVER_IP : EmulatorConfig.WIFI_SERVER_IP;

      try {
        if (EmulatorConfig.Instance.PHONE_EVENT_MODE == EmulatorConfig.Mode.USB) {
          setupPortForwarding(kPhoneEventPort);
        }
        TcpClient tcpClient = new TcpClient(addr, kPhoneEventPort);
        connected = true;
        ProcessConnection(tcpClient);
        tcpClient.Close();
      } finally {
        connected = false;
      }
    }

    private void setupPortForwarding(int port) {
#if !UNITY_WEBPLAYER
      string adbCommand = string.Format("adb forward tcp:{0} tcp:{0}", port);
      System.Diagnostics.Process myProcess = new System.Diagnostics.Process();

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
      string processFilename = "CMD.exe";
      string processArguments = @"/k " + adbCommand + " & exit";

      // See "Common Error Lookup Tool" (https://www.microsoft.com/en-us/download/details.aspx?id=985)
      // MSG_DIR_BAD_COMMAND_OR_FILE (cmdmsg.h)
      int kExitCodeCommandNotFound = 9009; // 0x2331

#else
      string processFilename = "bash";
      string processArguments = string.Format(" -c \"{0}\"", adbCommand);

      // "command not found" (see http://tldp.org/LDP/abs/html/exitcodes.html)
      int kExitCodeCommandNotFound = 127;
#endif // UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

      System.Diagnostics.ProcessStartInfo myProcessStartInfo =
        new System.Diagnostics.ProcessStartInfo(processFilename, processArguments);
      myProcessStartInfo.UseShellExecute = false;
      myProcessStartInfo.RedirectStandardError = true;
      myProcessStartInfo.CreateNoWindow = true;
      myProcess.StartInfo = myProcessStartInfo;
      myProcess.Start();
      myProcess.WaitForExit();
      // Also wait for HasExited here, to avoid ExitCode access below occasionally throwing InvalidOperationException
      while (!myProcess.HasExited) {
        Thread.Sleep(1);
      }
      int exitCode = myProcess.ExitCode;
      string standardError = myProcess.StandardError.ReadToEnd();
      myProcess.Close();

      if (exitCode == 0) {
        // Port forwarding setup successfully.
        return;
      }

      if (exitCode == kExitCodeCommandNotFound) {
        // Caught by phoneEventSocketLoop.
        throw new Exception(
          "Android Debug Bridge (`adb`) command not found." +
          "\nVerify that the Android SDK is installed and that the directory containing" +
          " `adb` is included in your PATH environment variable.");
      }
      // Caught by phoneEventSocketLoop.
      throw new Exception(
        String.Format(
          "Failed to setup port forwarding." +
          " Exit code {0} returned by process: {1} {2}\n{3}",
          exitCode, processFilename, processArguments, standardError));
#endif  // !UNITY_WEBPLAYER
    }

    private void ProcessConnection(TcpClient tcpClient) {
      byte[] buffer = new byte[4];
      NetworkStream stream = tcpClient.GetStream();
      stream.ReadTimeout = kSocketReadTimeoutMillis;
      tcpClient.ReceiveTimeout = kSocketReadTimeoutMillis;
      while (!shouldStop) {
        int bytesRead = blockingRead(stream, buffer, 0, 4);
        if (bytesRead < 4) {
          // Caught by phoneEventSocketLoop.
          throw new Exception(
            "Failed to read from controller emulator app event socket." +
            "\nVerify that the controller emulator app is running.");
        }
        int msgLen = unpack32bits(correctEndianness(buffer), 0);

        byte[] dataBuffer = new byte[msgLen];
        bytesRead = blockingRead(stream, dataBuffer, 0, msgLen);
        if (bytesRead < msgLen) {
          // Caught by phoneEventSocketLoop.
          throw new Exception(
            "Failed to read from controller emulator app event socket." +
            "\nVerify that the controller emulator app is running.");
        }

        PhoneEvent proto =
            PhoneEvent.CreateBuilder().MergeFrom(dataBuffer).Build();
        phoneRemote.OnPhoneEvent(proto);

        if (!lastConnectionAttemptWasSuccessful) {
          Debug.Log("Successfully connected to controller emulator app.");
          // Log first failure after above successful read from event socket.
          lastConnectionAttemptWasSuccessful = true;
        }
      }
    }

    private int blockingRead(NetworkStream stream, byte[] buffer, int index,
        int count) {
      int bytesRead = 0;
      while (!shouldStop && bytesRead < count) {
        try {
          int n = stream.Read(buffer, index + bytesRead, count - bytesRead);
          if (n <= 0) {
            // Failed to read.
            return -1;
          }
          bytesRead += n;
        } catch (IOException) {
          // Read failed or timed out.
          return -1;
        } catch (ObjectDisposedException) {
          // Socket closed.
          return -1;
        }
      }
      return bytesRead;
    }

    void OnDestroy() {
      shouldStop = true;

      if (phoneMirroringSocket != null) {
        phoneMirroringSocket.Close ();
        phoneMirroringSocket = null;
      }

      if (phoneEventThread != null) {
        phoneEventThread.Join();
      }
    }

    private int unpack32bits(byte[] array, int offset) {
      int num = 0;
      for (int i = 0; i < 4; i++) {
        num += array [offset + i] << (i * 8);
      }
      return num;
    }

    static private byte[] correctEndianness(byte[] array) {
      if (BitConverter.IsLittleEndian)
        Array.Reverse(array);

      return array;
    }
  }
}
/// @endcond
