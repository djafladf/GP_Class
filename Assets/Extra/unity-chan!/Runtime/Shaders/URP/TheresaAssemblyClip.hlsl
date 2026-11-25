#ifndef THERESA_ASSEMBLY_CLIP
#define THERESA_ASSEMBLY_CLIP
float _AssemblyEnabled, _AssemblyProgress, _AssemblyBuild, _AssemblyDirection;
float _AssemblyCell, _AssemblyNoise;
float4x4 _AssemblyVP;
float4 _AssemblyScreen; // width, height, left, right in camera viewport
float _AssemblySpatial, _AssemblyCell3D;
float4x4 _AssemblyWorldToGrid, _AssemblyGridToWorld;
float4 _AssemblyAxis; // normalized world direction, projected half width
float4 _AssemblyOrigin; // character bounds center in world space
float AssemblyThreshold(float3 world) {
 if(_AssemblySpatial>0.5) {
  float size=max(_AssemblyCell3D,0.001);
  float3 local=mul(_AssemblyWorldToGrid,float4(world,1)).xyz;
  float3 cell=floor(local/size);
  float3 center=mul(_AssemblyGridToWorld,float4((cell+0.5)*size,1)).xyz;
  float halfWidth=max(_AssemblyAxis.w,0.00001);
  float along=saturate((dot(center-_AssemblyOrigin.xyz,_AssemblyAxis.xyz)+halfWidth)/(2*halfWidth));
  float3 bounded=cell-floor(cell/257.0)*257.0;
  float h=bounded.x*17+bounded.y*131+bounded.z*71+23;
  h=h-floor(h/257.0)*257.0;
  float n=h*h*13+h*7;
  n=(n-floor(n/257.0)*257.0)/257.0;
  return 0.2+0.6*((1-_AssemblyNoise)*along+_AssemblyNoise*n);
 }
 float4 q=mul(_AssemblyVP,float4(world,1));
 float2 uv=q.xy/max(q.w,0.00001)*0.5+0.5;
 float2 cell=floor(uv*_AssemblyScreen.xy/max(_AssemblyCell,1));
 float2 center=(cell+0.5)*max(_AssemblyCell,1)/_AssemblyScreen.xy;
 float x=saturate((center.x-_AssemblyScreen.z)/max(_AssemblyScreen.w-_AssemblyScreen.z,0.00001));
 x=lerp(x,1-x,_AssemblyDirection);
 float h=cell.x*17+cell.y*131+23;
 h=h-floor(h/257.0)*257.0;
 float n=h*h*13+h*7;
 n=(n-floor(n/257.0)*257.0)/257.0;
 return 0.2+0.6*((1-_AssemblyNoise)*x+_AssemblyNoise*n);
}
void AssemblyClip(float3 world) {
 if(_AssemblyEnabled<0.5)return;
 float t=AssemblyThreshold(world);
 clip(_AssemblyBuild>0.5 ? _AssemblyProgress-t : t-_AssemblyProgress-0.000001);
}
#endif
