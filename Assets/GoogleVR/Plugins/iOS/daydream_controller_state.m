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

#import <math.h>
#import <GLKit/GLKit.h>
#import "daydream_controller_state.h"

#define QUATERNION_IDENTITY (gvr_quat){ GLKQuaternionIdentity.x, GLKQuaternionIdentity.y, GLKQuaternionIdentity.z, GLKQuaternionIdentity.w }
#define ORIENTATION_SCALE (2.0 * M_PI / 4095.0)
#define ACCELEROMETER_SCALE (9.8 * 8.0 / 4095.0)
#define GYRO_SCALE (M_PI * 2048.0 / 4095.0 / 180.0)

daydream_controller_state get_initial_daydream_controller_state() {

  return (daydream_controller_state) {
    .connectionState = GVR_CONTROLLER_DISCONNECTED,
    .orientation = QUATERNION_IDENTITY,
    .accel = (gvr_vec3){ 0, 0, 0 },
    .gyro = (gvr_vec3){ 0, 0, 0 },
    .touchPos = (gvr_vec2){ 0, 0 },
    .isTouching = false,
    .appButtonState = false,
    .homeButtonState = false,
    .clickButtonState = false,
    .plusButtonState = false,
    .minusButtonState = false,
    .supportsBatteryStatus = false,
    .batteryLevelPercentage = 0
  };
}

daydream_controller_state get_next_daydream_controller_state(uint8_t *data, daydream_controller_state previousState) {

  // raw orientation 13 bit signed int
  int16_t ox = (int16_t)(((uint16_t)data[1] << 14) | ((uint16_t)data[2] << 6) | ((uint16_t)(data[3] & 0b11100000) >> 2)) >> 3;
  int16_t oy = (int16_t)(((uint16_t)data[3] << 11) | ((uint16_t)data[4] << 3)) >> 3;
  int16_t oz = (int16_t)(((uint16_t)data[5] <<  8) | ((uint16_t)(data[6] & 0b11111000))) >> 3;
  // raw accelerometer 13 bit signed int
  int16_t ax = (int16_t)(((uint16_t)data[6] << 13) | ((uint16_t)data[7] << 5) | ((uint16_t)(data[8] & 0b11000000) >> 1)) >> 3;
  int16_t ay = (int16_t)(((uint16_t)data[8] << 10) | ((uint16_t)(data[9] & 0b11111110) << 2)) >> 3;
  int16_t az = (int16_t)(((uint16_t)data[9] << 15) | ((uint16_t)data[10] << 7) | ((uint16_t)(data[11] & 0b11110000) >> 1)) >> 3;
  // raw gyroscope 13 bit signed int, unit = 0.5 * degrees per second
  int16_t gx = (int16_t)(((uint16_t)data[11] << 12) | ((uint16_t)data[12] << 4) | ((uint16_t)(data[13] & 0b10000000) >> 4)) >> 3;
  int16_t gy = (int16_t)(((uint16_t)data[13] <<  9) | ((uint16_t)(data[14] & 0b11111100) << 1)) >> 3;
  int16_t gz = (int16_t)(((uint16_t)data[14] << 14) | ((uint16_t)data[15] << 6) | ((uint16_t)(data[16] & 0b11100000) >> 2)) >> 3;
  // touchpad x, y with 0 - 255
  uint8_t tx = (data[16] << 3) | (data[17] >> 5);
  uint8_t ty = (data[17] << 3) | (data[18] >> 5);
  // buttons
  uint8_t buttonFlags = data[18] & 0b00011111;
  // last byte unknown or reserved

  // orientation is represented as axis-angles with magnitude as rotation angle theta around that vector
  float x = (float)ox * ORIENTATION_SCALE;
  float y = (float)oy * ORIENTATION_SCALE;
  float z = (float)oz * ORIENTATION_SCALE;
  float magnitudeSquare = x * x + y * y + z * z;

  gvr_quat orientation;
  if (0.0 < magnitudeSquare) {
    float magnitude = sqrt(magnitudeSquare); // same as axis angle
    float scale = 1.0 / magnitude;
    GLKQuaternion glkOrientation = GLKQuaternionMakeWithAngleAndAxis(magnitude, x * scale, y * scale, z * scale);
    orientation = (gvr_quat){ glkOrientation.x, glkOrientation.y, glkOrientation.z, glkOrientation.w };
  } else {
    orientation = QUATERNION_IDENTITY;
  }

  gvr_vec3 accel = (gvr_vec3) {
    .x = (float)ax * ACCELEROMETER_SCALE,
    .y = (float)ay * ACCELEROMETER_SCALE,
    .z = (float)az * ACCELEROMETER_SCALE
  };

  gvr_vec3 gyro = (gvr_vec3) {
    .x = (float)gx * GYRO_SCALE,
    .y = (float)gy * GYRO_SCALE,
    .z = (float)gz * GYRO_SCALE
  };

  // The touch position from upper left (0, 0) to lower right (1, 1).
  gvr_vec2 touchPos = (gvr_vec2) {
    .x = (float)tx / 255.0,
    .y = (float)ty / 255.0
  };

  daydream_controller_state state = (daydream_controller_state) {
    .connectionState = previousState.connectionState,
    .orientation = orientation,
    .accel = accel,
    .gyro = gyro,
    .touchPos = touchPos,
    .isTouching = (0 < tx || 0 < ty),
    .clickButtonState = (0 < (buttonFlags & 0b00001)),
    .homeButtonState  = (0 < (buttonFlags & 0b00010)),
    .appButtonState   = (0 < (buttonFlags & 0b00100)),
    .plusButtonState  = (0 < (buttonFlags & 0b01000)),
    .minusButtonState = (0 < (buttonFlags & 0b10000)),
    .supportsBatteryStatus = previousState.supportsBatteryStatus,
    .batteryLevelPercentage = previousState.batteryLevelPercentage
  };

  return state;
}
