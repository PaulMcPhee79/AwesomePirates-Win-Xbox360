
/* Solution 1: See link for settings: http://www.geeks3d.com/20110428/shader-library-swirl-post-processing-filter-in-glsl/
   Explained: http://adrianboeing.blogspot.com.au/2011/01/twist-effect-in-webgl.html
[Vertex_Shader]
void main()
{	
  gl_Position = ftransform();		
  gl_TexCoord[0] = gl_MultiTexCoord0;
}

[Pixel_Shader]
// Scene buffer
uniform sampler2D tex0; 

// Currently not used in this demo!
uniform float time; 

// GeeXLab built-in uniform, width of
// the current render target
uniform float rt_w; 
// GeeXLab built-in uniform, height of
// the current render target
uniform float rt_h; 

// Swirl effect parameters
uniform float radius = 200.0;
uniform float angle = 0.8;
uniform vec2 center = vec2(400.0, 300.0);

vec4 PostFX(sampler2D tex, vec2 uv, float time)
{
  vec2 texSize = vec2(rt_w, rt_h);
  vec2 tc = uv * texSize;
  tc -= center;
  float dist = length(tc);
  if (dist < radius) 
  {
    float percent = (radius - dist) / radius;
    float theta = percent * percent * angle * 8.0;
    float s = sin(theta);
    float c = cos(theta);
    tc = vec2(dot(tc, vec2(c, -s)), dot(tc, vec2(s, c)));
  }
  tc += center;
  vec3 color = texture2D(tex0, tc / texSize).rgb;
  return vec4(color, 1.0);
}

void main (void)
{
  vec2 uv = gl_TexCoord[0].st;
  gl_FragColor = PostFX(tex0, uv, time);
}
*/


// =============================================================================


/* Solution 2
You will need to work in Polar Coordinates. In order to convert Cartesian Coordinates into Polar you use the following:

r = sqrt(x * x + y * y);
theta = atan2(y, x);
This will get the coordinates around {0, 0} - so we need to change the Cartesian origin before converting:

float mx = x - 0.5;
float my = y - 0.5;
float r = sqrt(mx * mx + my * my);
float theta = atan2(y, x);
Once you have the polar coordinates you can use a typical multi-tap blur to perform the blur.

const int SAMPLES = 15;
extern float PARAM_RadialBlurLength = 0.01f; // In radians

float2 offsetCoord = inputTexCoord - float2(0.5, 0.5);
float r = length(offsetCoord);
float theta = atan2(offsetCoord.y, offsetCoord.x);

float4 sum = (float4)0;
float2 tapCoords = (float2)0;
for(float i = 0; i < SAMPLES; i++)
{
  float tapTheta = theta + i * (PARAM_radialBlurLength / SAMPLES);
  float tapR = r; // If you alter this according to 'i' you will get a swirl.

  // Remember we need to invert the origin offset we 
  // used to create the co-ordinates, the the + 0.5 on each.
  tapCoords.x = tapR * cos(tapTheta) + 0.5;
  tapCoords.y = tapR * sin(tapTheta) + 0.5;

  sum += tex2D(inputSampler, tapCoords);
}
sum /= SAMPLES;
return sum;
*/

// =============================================================================

/* Rotation Solution
float4x4 WorldViewProj : WorldViewProjection;
float4x4 World : World;
float3 eyepos : CameraPosition;
float fTime : Time;
float STime : SinTime;

float distortSize
<
	string UIWidget = "slider";
	float UIMax = 0.2;
	float UIMin = 0.0;
	float UIStep = 0.001;
> = 0.05;

float distortSpeed
<
	string UIWidget = "slider";
	float UIMax = 0.4;
	float UIMin = 0.0;
	float UIStep = 0.001;
> = 0.1;

float Rotations
<
	string UIWidget = "slider";
	float UIMax = 5.0;
	float UIMin = 0.0;
	float UIStep = 0.001;
> = 0.05;

float Offset
<
	string UIWidget = "slider";
	float UIMax = 5.0;
	float UIMin = 0.0;
	float UIStep = 0.001;
> = 0.05;

float Normality
<
	string UIWidget = "slider";
	float UIMax = 25.0;
	float UIMin = 0.0;
	float UIStep = 0.001;
> = 0.05;

texture Background : DIFFUSE
<
	string ResourceName = "";
	string Type = "2D";
	string Dynamic = "Background";
>;

sampler texture1 = sampler_state
{
	Texture = <Background>;
	MinFilter = Anisotropic;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Clamp;
	AddressV = Clamp;
};

texture DistortTex : DIFFUSE
<
	string ResourceName = "";
	string Type = "2D";
>;

sampler distortSmp = sampler_state
{
	Texture = <DistortTex>;
	MinFilter = Anisotropic;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
};

struct app_in
{
	float4 pos : POSITION;
	float3 uv : TEXCOORD0;
	float4 normal :NORMAL;
};

struct vs_out
{
	float4 pos : POSITION;
	float3 uv : TEXCOORD0;
	float4 projUV : TEXCOORD1;
	float4 normal : TEXCOORD2;
	float4 wpos : TEXCOORD3;
};

vs_out DefaultVertexShader( app_in IN )
{
	vs_out OUT;
	
	OUT.pos = mul( IN.pos, WorldViewProj );
	OUT.wpos = mul (IN.pos, World);
	OUT.uv = IN.uv;
	OUT.normal=IN.normal;
	OUT.projUV = float4( OUT.pos.x*0.5 + 0.5*OUT.pos.w, 0.5*OUT.pos.w - OUT.pos.y*0.5, OUT.pos.w, OUT.pos.w );
	
	return OUT;
}

float4 DefaultPixelShader( vs_out IN ) : COLOR
{
	//create an offset from the time and variable values
	float2 offset = float2( sin(fTime*distortSpeed), cos(fTime*distortSpeed) );
	//convert into 0-1 range
	float3 uv = IN.projUV.xyz / IN.projUV.w;
	float3 center = {IN.projUV.x, IN.projUV.y, IN.projUV.z};
	center.y=IN.normal.y*Rotations-(atan(fTime)*Rotations);
	center.x=IN.normal.x*Rotations-(sin(fTime-Offset)*Rotations);
	center.z=IN.normal.z*Rotations-(cos(fTime-Offset)*Rotations);
	float3 Center=mul(center, IN.wpos.xyz/Offset)*Normality;
	uv = uv + (tex2D(distortSmp, Center ) - 0.5) * distortSize;

	float4 UVRotation=(tex2D(texture1, uv));
	return UVRotation;
}

technique MyShader
{
	pass p0
	{
		VertexShader = compile vs_1_1 DefaultVertexShader( );
		PixelShader = compile ps_2_0 DefaultPixelShader( );
	}
}
*/

