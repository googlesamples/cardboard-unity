// Copyright 2017 Google Inc. All rights reserved.
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
// See the License for the specific language governing permissions and
// limitations under the License.

#import <stdint.h>

typedef struct {
  float x;
  float y;
  float z;
  float w;
} gvr_quat;

typedef struct {
  float x;
  float y;
  float z;
} gvr_vec3;

typedef struct {
  float x;
  float y;
} gvr_vec2;

typedef enum {
  GVR_CONTROLLER_DISCONNECTED = 0,
  GVR_CONTROLLER_SCANNING = 1,
  GVR_CONTROLLER_CONNECTING = 2,
  GVR_CONTROLLER_CONNECTED = 3
} controller_connection_state;

typedef struct {
  controller_connection_state connectionState;
  gvr_quat orientation;
  gvr_vec3 accel;
  gvr_vec3 gyro;
  gvr_vec2 touchPos;
  bool isTouching;
  bool appButtonState;
  bool homeButtonState;
  bool clickButtonState;
  bool plusButtonState;
  bool minusButtonState;
  bool supportsBatteryStatus;
  uint8_t batteryLevelPercentage;
} daydream_controller_state;

daydream_controller_state get_initial_daydream_controller_state();

// `data` is a pointer to a 20-byte buffer with the Bluetooth data from the controller
daydream_controller_state get_next_daydream_controller_state(UInt8 *data, daydream_controller_state previousState);
