// Copyright 2014 Google Inc. All rights reserved.
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


half4 fog(half distanceToCamera,
    half3 normVector, 
        half4 _ZenithColor,
        half4 _ZenithFog,

        half4 _HorizonColor,
        half4 _HorizonFog,
        half4 _FogData,
        half4 _PrimaryAmbientColor,
        half4 _DirectionalData,
        half4 _DirectionalColor,
        half4 _DirectionalFog) {

    half fogVertical = saturate(normVector.y);

    //We can skip the normalize on the norm vector - it's not right, but, it's close, and I need the instructions back
    half fogAngle = _DirectionalData.x * saturate((dot(_DirectionalData.zw,(normVector.xz) )));//(atan2(normVector.z, normVector.x) / (2 * 3.1415926) ) + 0.5;


    half horizonFogDistance = distanceToCamera * _HorizonColor.a;
    half zenithFogDistance = distanceToCamera * _ZenithColor.a;
    half directionalFogDistance = distanceToCamera * _DirectionalData.y;

    horizonFogDistance = (1-fogAngle)* horizonFogDistance + directionalFogDistance * fogAngle;



    ///We are using the vertical vector directly, we can modify this math later
    half fogAlpha = saturate(horizonFogDistance + fogVertical * zenithFogDistance*zenithFogDistance );


    horizonFogDistance = saturate( (horizonFogDistance - _HorizonFog.a)*_FogData.x );
    directionalFogDistance = saturate( (directionalFogDistance - _FogData.w)*_FogData.z );
    horizonFogDistance = (1-fogAngle)* horizonFogDistance + directionalFogDistance * fogAngle;
    half3 horizonColor = horizonFogDistance * ( (1-fogAngle)*_HorizonColor.rgb + fogAngle*_DirectionalColor.rgb) + (1-horizonFogDistance)*( (1-fogAngle)*_HorizonFog.rgb + fogAngle*_DirectionalFog.rgb);

    zenithFogDistance = saturate( (zenithFogDistance - _ZenithFog.a)*_FogData.y );
    half3 zenithColor = zenithFogDistance *_ZenithColor.rgb + (1-zenithFogDistance)*_ZenithFog.rgb;

    half3 fogColor = (1-fogVertical)*horizonColor + fogVertical*zenithColor;

    return half4(fogColor,fogAlpha);
}


half Shadow(sampler2D shadowTex, half3 shadowPoint){
    half4 compressedShadow = tex2D(shadowTex, shadowPoint.xy  );
    //half shadowScale = saturate( 10*( max(abs(fragment.shadowPoint.x),abs(fragment.shadowPoint.y) )-0.9) );
    float shadowDepth = DecodeFloatRGBA(compressedShadow);


    ///Exponential Shadow Maps
    const float k = 10;
    float expShadowPoint = exp(-k*shadowPoint.z);
    float shadowing =   saturate(expShadowPoint * exp(k*shadowDepth) );//shadowScale
    shadowing = saturate(shadowing + 5*saturate( max (abs(2*shadowPoint.x-1), abs(2*shadowPoint.y-1)  )-0.8  )- 5*saturate(shadowPoint.z-0.8) );
    return shadowing;
}
