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

using UnityEngine;
using System.Collections;

public static class Smoothing {

	const float HALF_PI = Mathf.PI *0.5f;

	public static float SpringSmooth(float from, float to, ref float velocity, float smoothTime, float dt){
		float delta = from - to;
		float omega = 2.0f/smoothTime;
		float x = omega*dt;
		float exp = 1.0f / Mathf.Exp(x);
		float temp = (velocity + omega*delta)*dt;
		velocity = (velocity - omega*temp)*exp;
		return (to + (delta+temp)*exp);
	}
	
	public static float SpringSmoothAngle(float from, float to, ref float velocity, float smoothTime, float dt){
		from = ((from%360)+360)%360;
		to = ((to%360)+360)%360;
		float delta = from - to;
		if(delta > 180){
			delta = delta - 360;
		}
		else if(delta < -180){
			delta = delta + 360;
		}
		
		float omega = 2.0f/smoothTime;
		float x = omega*dt;
		float exp = 1.0f / Mathf.Exp(x);
		float temp = (velocity + omega*delta)*dt;
		velocity = (velocity - omega*temp)*exp;
		return (to + (delta+temp)*exp);
	}
	
	
	public static float QuadraticEaseIn(float from, float to, ref float velocity,float acceleration, float dt){
		//this isnt ease in...
		float delta = to - from;
		velocity += Mathf.Sign(delta) * acceleration*2* dt; //2 from derivation
		return from+velocity*dt;
	}
	
	public static float QuadraticEaseOut(float from, float to, ref float velocity,float acceleration, float dt){
		
		float delta = to - from;
		
		velocity = 2*delta*acceleration;
		return from+velocity*dt;
	}
	
	
	/* 
	Position = delta * time^2 + start 
	Velocity = delta * time * 2
	*/
	public static float QuadraticEaseIn(float normalizedTime){
		
		if(normalizedTime <= 0){
			return 0;	
		}
		else if(normalizedTime >= 1){
			return 1;	
		}

		
		return normalizedTime*normalizedTime;
	}
	
	/* 
	Position = delta*( 1 - (time-1)^2 ) + start
	Position = delta*(-time^2 + 2*time )+ start
	Position = delta*time*(2-time) + start
	Velocity = delta*(2 - 2*time)
	*/
	public static float QuadraticEaseOut(float normalizedTime){
		
		if(normalizedTime <= 0){
			return 0;	
		}
		else if(normalizedTime >= 1){
			return 1;	
		}
		
		float factor = (1-normalizedTime);
		return (1-factor*factor);
	}
	
	
	/* 
	2*(x)^2
	-2*(x-1)^2 + 1
	-2x^2 + 4x -2 + 1
	-2x^2 + 4x -1
	(2x*(2-x) -1)
	
	*/
	public static float QuadraticEaseInOut(float normalizedTime){
		
		if(normalizedTime <= 0){
			return 0;	
		}
		else if(normalizedTime >= 1){
			return 1;	
		}
		
		
		
		if(normalizedTime < 0.5f){
			//Return ease in, *2
			return 2*normalizedTime*normalizedTime;
		}
		else{
			
			//return ease out
			float factor = (1 - normalizedTime);
			return (1-2*factor*factor);
		}
		

	}
	
	public static float CubicEaseIn(float normalizedTime){
		
		if(normalizedTime <= 0){
			return 0;	
		}
		else if(normalizedTime >= 1){
			return 1;	
		}
		
		
		return normalizedTime*normalizedTime*normalizedTime;
	}
	/* 
	Position = delta*( 1 - (time-1)^3 ) + start
	
	*/
	public static float CubicEaseOut(float normalizedTime){
		
		if(normalizedTime <= 0){
			return 0;	
		}
		else if(normalizedTime >= 1){
			return 1;	
		}
		
		
		
		float factor = (1-normalizedTime);
		return (1-factor*factor*factor);
	}
	
	public static float CubicEaseInOut(float normalizedTime){
		
		if(normalizedTime <= 0){
			return 0;	
		}
		else if(normalizedTime >= 1){
			return 1;	
		}
		
		
		
		if(normalizedTime < 0.5f){
			//Return ease in, *8 / 2
			return 4*normalizedTime*normalizedTime*normalizedTime;
		}
		else{
			
			//return ease out
			float factor = (1 - normalizedTime);
			return (1-4*factor*factor*factor);
		}
	}
	
	public static float QuarticEaseIn(float normalizedTime){
		
		if(normalizedTime <= 0){
			return 0;	
		}
		else if(normalizedTime >= 1){
			return 1;	
		}
		
		
		return normalizedTime*normalizedTime*normalizedTime*normalizedTime;
	}
	public static float QuarticEaseOut(float normalizedTime){
		
		if(normalizedTime <= 0){
			return 0;	
		}
		else if(normalizedTime >= 1){
			return 1;	
		}
		
		
		
		float factor = (1 - normalizedTime);
		return (1-factor*factor*factor*factor);
	}
	public static float QuarticEaseInOut(float normalizedTime){
		
		if(normalizedTime <= 0){
			return 0;	
		}
		else if(normalizedTime >= 1){
			return 1;	
		}
		

		if(normalizedTime < 0.5f){
			//Return ease in, *16 / 2
			return 8*normalizedTime*normalizedTime*normalizedTime*normalizedTime;
		}
		else{
			
			//return ease out
			float factor = (1 - normalizedTime);
			return (1-8*factor*factor*factor*factor);
		}
	}
	
	public static float QuinticEaseIn(float normalizedTime){
		
		if(normalizedTime <= 0){
			return 0;	
		}
		else if(normalizedTime >= 1){
			return 1;	
		}
		
		
		return normalizedTime*normalizedTime*normalizedTime*normalizedTime*normalizedTime;
	}
	public static float QuinticEaseOut(float normalizedTime){
		
		if(normalizedTime <= 0){
			return 0;	
		}
		else if(normalizedTime >= 1){
			return 1;	
		}
		
		
		float factor = (1 - normalizedTime);
		return (1-factor*factor*factor*factor*factor);
	}
	public static float QuinticEaseInOut(float normalizedTime){
		
		if(normalizedTime <= 0){
			return 0;	
		}
		else if(normalizedTime >= 1){
			return 1;	
		}
		

		if(normalizedTime < 0.5f){
			//Return ease in, *32 / 2
			return 16*normalizedTime*normalizedTime*normalizedTime*normalizedTime*normalizedTime;
		}
		else{
			
			//return ease out
			float factor = (1 - normalizedTime);
			return (1-16*factor*factor*factor*factor*factor);
		}
	}
	public static float SinusoidalEaseIn(float normalizedTime){
		
		if(normalizedTime <= 0){
			return 0;	
		}
		else if(normalizedTime >= 1){
			return 1;	
		}
		
		return ( 1-Mathf.Cos(normalizedTime*HALF_PI) );
	}
	public static float SinusoidalEaseOut(float normalizedTime){
		
		if(normalizedTime <= 0){
			return 0;	
		}
		else if(normalizedTime >= 1){
			return 1;	
		}
		
		return Mathf.Sin(normalizedTime*HALF_PI);
	}
	
	public static float SinusoidalEaseInOut(float normalizedTime){
		
		if(normalizedTime <= 0){
			return 0;	
		}
		else if(normalizedTime >= 1){
			return 1;	
		}
		
		return -0.5f*( Mathf.Cos(normalizedTime*Mathf.PI)-1 );
	}
	
	public static float CircularEaseIn(float normalizedTime){
		
		if(normalizedTime <= 0){
			return 0;	
		}
		else if(normalizedTime >= 1){
			return 1;	
		}

		return (1 - Mathf.Sqrt(1 - normalizedTime * normalizedTime) );
	}
	public static float CircularEaseOut(float normalizedTime){
		
		if(normalizedTime <= 0){
			return 0;	
		}
		else if(normalizedTime >= 1){
			return 1;	
		}
		
		normalizedTime = normalizedTime-1;
		return Mathf.Sqrt(1 - normalizedTime * normalizedTime);
	}
	public static float CircularEaseInOut(float normalizedTime){
		
		if(normalizedTime <= 0){
			return 0;	
		}
		else if(normalizedTime >= 1){
			return 1;	
		}
	
		
		if(normalizedTime < 0.5f){
			//Return ease in, *2
			normalizedTime = 2*normalizedTime;
			normalizedTime = 1- normalizedTime * normalizedTime;
			if(normalizedTime > 0){
				return 0.5f* (1 - Mathf.Sqrt(normalizedTime) );
			}
			else{
				return 0.5f;
			}
		}
		else{
			
			//return ease out
			normalizedTime = 2*(1-normalizedTime);
			normalizedTime = 1- normalizedTime * normalizedTime;
			if(normalizedTime > 0){
				return 0.5f* Mathf.Sqrt(normalizedTime) +0.5f;
			}
			else{
				return 1;
			}
		}
	}
	
	public static float ExponentialEaseIn(float normalizedTime){
		
		if(normalizedTime <= 0){
			return 0;	
		}
		else if(normalizedTime >= 1){
			return 1;	
		}
		
	
		return Mathf.Exp( 10 * (normalizedTime - 1) );
	}
	public static float ExponentialEaseOut(float normalizedTime){
		
		if(normalizedTime <= 0){
			return 0;	
		}
		else if(normalizedTime >= 1){
			return 1;	
		}
		

		return  ( 1 - Mathf.Exp( -10 * (normalizedTime) ) );

	}
	public static float ExponentialEaseInOut( float normalizedTime){
		
		if(normalizedTime <= 0){
			return 0;	
		}
		else if(normalizedTime >= 1){
			return 1;	
		}
		

		
		if(normalizedTime < 0.5f){
			//Return ease in, *2
			
			return  0.5f * Mathf.Exp(10 * (2*normalizedTime - 1) ) ;
		}
		else{
			
			//return ease out

			return  0.5f * ( 2 - Mathf.Exp(-10 * (2*normalizedTime -1) ) ) ;
		}
	}
	

}
