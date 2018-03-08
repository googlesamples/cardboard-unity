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

#import <CoreBluetooth/CoreBluetooth.h>
#import "GvrDaydreamController.h"
#import "daydream_controller_state.h"

#define BATTERY_LEVEL_CHARACTERISTIC_UUID @"2A19"
#define BATTERY_SERVICE_UUID @"180F"
#define DAYDREAM_CHARACTERISTIC_UUID @"00000001-1000-1000-8000-00805f9b34fb"
#define DAYDREAM_SERVICE_UUID @"0000fe55-0000-1000-8000-00805f9b34fb"

GvrDaydreamController *_sharedInstance = nil;

@implementation GvrDaydreamController {
  CBUUID *_batteryLevelCharacteristicUuid;
  CBUUID *_batteryServiceUuid;
  CBUUID *_daydreamCharacteristicUuid;
  CBUUID *_daydreamServiceUuid;
  CBCentralManager *_manager;
  CBPeripheral *_peripheral;
  CBCharacteristic *_characteristic;
  CBCharacteristic *_batteryLevelCharacteristic;
  daydream_controller_state _state;
}

- (id)init {

  if (self = [super init]) {
    _batteryLevelCharacteristicUuid = [CBUUID UUIDWithString:BATTERY_LEVEL_CHARACTERISTIC_UUID];
    _batteryServiceUuid = [CBUUID UUIDWithString:BATTERY_SERVICE_UUID];
    _daydreamCharacteristicUuid = [CBUUID UUIDWithString:DAYDREAM_CHARACTERISTIC_UUID];
    _daydreamServiceUuid = [CBUUID UUIDWithString:DAYDREAM_SERVICE_UUID];
    _manager = nil;
    [self _clearControllerState];
  }
  return self;
}

- (void)disconnect {

  [self pause];
  if (_peripheral && _manager) {
    [_manager cancelPeripheralConnection:_peripheral];
    [self _clearControllerState];
  }
}

- (daydream_controller_state)getState {

  return _state;
}

- (void)pause {

  [self _setNotifyValues:false];
}

- (void)resume {

  [self _setNotifyValues:true];
}

- (void)start {

  if (!_manager) {
    dispatch_queue_attr_t qosAttribute = dispatch_queue_attr_make_with_qos_class(DISPATCH_QUEUE_CONCURRENT, QOS_CLASS_USER_INTERACTIVE, 0);
    dispatch_queue_t queue = dispatch_queue_create("vr.daydreamcontroller", qosAttribute);
    _manager = [[CBCentralManager alloc] initWithDelegate:self queue:queue];
  }
  if (_peripheral) {
    if (_peripheral.state == CBPeripheralStateDisconnected) {
      [self _connectPeripheral:_peripheral];
    } else {
      [self resume];
    }
  }
}

+ (GvrDaydreamController *)sharedInstance {

  if (!_sharedInstance) {
    _sharedInstance = [GvrDaydreamController new];
  }
  return _sharedInstance;
}

// see CBCentralManagerDelegate
- (void)centralManagerDidUpdateState:(CBCentralManager *)centralManager {

  if (centralManager.state == CBCentralManagerStatePoweredOn) {
    NSArray *connectedPeripherals = [centralManager retrieveConnectedPeripheralsWithServices:@[_daydreamServiceUuid]];
    if ([connectedPeripherals count]) {
      [self _handleDiscoveredPeripheral:connectedPeripherals[0]];
      return;
    }
    [centralManager scanForPeripheralsWithServices:@[_daydreamServiceUuid] options: nil];
  } else {
    [self _clearControllerState];
  }
}

// see CBCentralManagerDelegate
- (void)centralManager:(CBCentralManager *)centralManager
    didDiscoverPeripheral:(CBPeripheral *)peripheral
    advertisementData:(NSDictionary<NSString *,id> *)advertisementData
    RSSI:(NSNumber *)RSSI {

  [_manager stopScan];
  [self _handleDiscoveredPeripheral:peripheral];
}

// see CBCentralManagerDelegate
- (void)centralManager:(CBCentralManager *)centralManager didConnectPeripheral:(CBPeripheral *)peripheral {

  _state.connectionState = GVR_CONTROLLER_CONNECTED;
  [peripheral discoverServices:@[_daydreamServiceUuid, _batteryServiceUuid]];
}

// see CBCentralManagerDelegate
- (void)centralManager:(CBCentralManager *)centralManager didDisconnectPeripheral:(CBPeripheral *)peripheral error:(NSError *)error {

  [self _clearControllerState];
}

// see CBPeripheralDelegate
- (void)centralManager:(CBCentralManager *)centralManager didFailToConnect:(CBPeripheral *)peripheral error:(NSError *)error {

  [self _clearControllerState];
}

// see CBPeripheralDelegate
- (void)peripheral:(CBPeripheral *)peripheral didDiscoverServices:(NSError *)error {

  for (CBService *service in peripheral.services) {
    if ([service.UUID isEqual:_daydreamServiceUuid]) {
      [peripheral discoverCharacteristics:@[_daydreamCharacteristicUuid] forService:service];
    } else if ([service.UUID isEqual:_batteryServiceUuid]) {
      [peripheral discoverCharacteristics:@[_batteryLevelCharacteristicUuid] forService:service];
    }
  }
}

// see CBPeripheralDelegate
- (void)peripheral:(CBPeripheral *)peripheral didDiscoverCharacteristicsForService:(CBService *)service error:(NSError *)error {

  for (CBCharacteristic *characteristic in service.characteristics) {
    if ([characteristic.UUID isEqual:_daydreamCharacteristicUuid]) {
      _characteristic = characteristic;
      [_peripheral setNotifyValue:true forCharacteristic:characteristic];
    } else if ([characteristic.UUID isEqual:_batteryLevelCharacteristicUuid]) {
      _state.supportsBatteryStatus = true;
      _batteryLevelCharacteristic = characteristic;
      [_peripheral readValueForCharacteristic:characteristic];
      [_peripheral setNotifyValue:true forCharacteristic:characteristic];
    }
  }
}

// see CBPeripheralDelegate
- (void)peripheral:(CBPeripheral *)peripheral didUpdateValueForCharacteristic:(CBCharacteristic *)characteristic error:(NSError *)error {

  if ([characteristic isEqual:_characteristic]) {
    NSData *sensorData = characteristic.value;
    if (sensorData.length != 20) {
      return;
    }
    _state = get_next_daydream_controller_state(sensorData.bytes, _state);
  } else if ([characteristic isEqual:_batteryLevelCharacteristic]) {
    NSData *batteryData = characteristic.value;
    if (batteryData.length != 1) {
      return;
    }
    _state.batteryLevelPercentage = ((UInt8 *)batteryData.bytes)[0];
  }
}

- (void)_clearControllerState {

  _peripheral = nil;
  _state = get_initial_daydream_controller_state();
}

- (void)_connectPeripheral:(CBPeripheral *)peripheral {

  _state.connectionState = GVR_CONTROLLER_CONNECTING;
  [_manager connectPeripheral:peripheral options:nil];
}

- (void)_handleDiscoveredPeripheral:(CBPeripheral *)peripheral {

  _peripheral = peripheral;
  _peripheral.delegate = self;
  [self _connectPeripheral:_peripheral];
}

- (void)_setNotifyValues:(BOOL)enabled {

  if (_peripheral && _characteristic) {
    [_peripheral setNotifyValue:enabled forCharacteristic:_characteristic];
  }
  if (_peripheral && _batteryLevelCharacteristic) {
    [_peripheral setNotifyValue:enabled forCharacteristic:_batteryLevelCharacteristic];
  }
}

@end
