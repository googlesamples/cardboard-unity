// Copyright 2015 Google Inc. All rights reserved.
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

#import "UnityAppController.h"
#import "UnityAppController+Rendering.h"
#import "UnityAppController+ViewHandling.h"

// Unity 4.6.2 added a category to the app controller.
#if UNITY_VERSION < 462
#import "UnityInterface.h"
#else
#import "UnityAppController+UnityInterface.h"
#endif

// Unity 4 used a different method name to create the UnityView.
#if UNITY_VERSION < 500
#define createUnityView initUnityViewImpl
#endif

@interface CardboardAppController : UnityAppController

- (UnityView *)createUnityView;

- (void)launchSettingsDialog;

- (void)startSettingsDialog:(UIViewController *)dialog;

- (void)stopSettingsDialog;

- (void)vrBackButtonPressed;

- (void)pause:(bool)paused;

// Override this method in a subclass to hook your own finishActivityAndReturn
// functionality.
- (void)finishActivityAndReturn;

@end
