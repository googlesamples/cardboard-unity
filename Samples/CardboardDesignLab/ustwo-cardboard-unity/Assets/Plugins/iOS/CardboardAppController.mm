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

#import "CardboardAppController.h"

extern "C" {

extern void VRBackButtonPressed();

extern void cardboardPause(bool paused);
extern void createUiLayer(id app, UIView* view);
extern UIViewController *createSettingsDialog(id app);
extern UIViewController *createOnboardingDialog(id app);

bool isOpenGLAPI() {
#if UNITY_VERSION < 463
  return true;
#else
  CardboardAppController *app = (CardboardAppController *)GetAppController();
  UnityRenderingAPI api = [app renderingAPI];
  return api == apiOpenGLES2 || api == apiOpenGLES3;
#endif
}

void launchSettingsDialog() {
  CardboardAppController *app = (CardboardAppController *)GetAppController();
  [app launchSettingsDialog];
}

// Prevent launching onboarding twice due to focus change when iOS asks
// user for permission to use camera.
static bool isOnboarding = false;

void launchOnboardingDialog() {
  if (isOnboarding) {
    return;
  }
  isOnboarding = true;
  CardboardAppController *app = (CardboardAppController *)GetAppController();
  [app startSettingsDialog:createOnboardingDialog(app)];
}

void endSettingsDialog() {
  CardboardAppController *app = (CardboardAppController *)GetAppController();
  [app stopSettingsDialog];
  isOnboarding = false;
}

float getScreenDPI() {
  return ([[UIScreen mainScreen] scale] > 2.0f) ? 401.0f : 326.0f;
}

void finishActivityAndReturn() {
  CardboardAppController *app = (CardboardAppController *)GetAppController();
  [app finishActivityAndReturn];
}

}  // extern "C"

@implementation CardboardAppController

- (UnityView *)createUnityView {
  UnityView* unity_view = [super createUnityView];
  createUiLayer(self, (UIView *)unity_view);
  return unity_view;
}

- (void)launchSettingsDialog {
  [self startSettingsDialog:createSettingsDialog(self)];
}

- (void)startSettingsDialog:(UIViewController*)dialog {
  [self pause:YES];
  [self.rootViewController presentViewController:dialog animated:NO completion:nil];
}

- (void)stopSettingsDialog {
  [[self rootViewController] dismissViewControllerAnimated:NO completion:nil];
  [self pause:NO];
}

- (void)vrBackButtonPressed {
  VRBackButtonPressed();
}

- (void)pause:(bool)paused {
#if UNITY_VERSION < 462
  UnityPause(paused);
#else
  self.paused = paused;
#endif
  cardboardPause(paused);
}

- (void)finishActivityAndReturn {
}

@end

IMPL_APP_CONTROLLER_SUBCLASS(CardboardAppController)
