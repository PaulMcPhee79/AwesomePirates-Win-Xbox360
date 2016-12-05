float4x4 Local;
float4x4 World;
float4x4 View;
float4x4 Projection;

float3 EyePos;

float time = 0;
float oceanAlpha = 0.125;
float waveScale = 5;

float textureLerp;
uniform const int kMaxAnisotropy = 4; // Lower for performance gains (8 default)
uniform const float4 kWaterColor = float4(1,1,1,1); // Water color

texture2D normalTex;
sampler2D NormalTextureSampler = sampler_state
{
    Texture = <normalTex>;
    MinFilter = anisotropic;
    MagFilter = anisotropic;
    MipFilter = linear;
	MaxAnisotropy = <kMaxAnisotropy>;
    AddressU = wrap;
    AddressV = wrap;
};

texture2D normalTex2;
sampler2D NormalTextureSampler2 = sampler_state
{
    Texture = <normalTex2>;
    MinFilter = anisotropic;
    MagFilter = anisotropic;
    MipFilter = linear;
	MaxAnisotropy = <kMaxAnisotropy>;
    AddressU = wrap;
    AddressV = wrap;
};

//texture2D maskTex;
//sampler2D MaskTextureSampler = sampler_state
//{
//    Texture = <maskTex>;
//    MinFilter = anisotropic;
//    MagFilter = anisotropic;
//    MipFilter = linear;
//	MaxAnisotropy = <kMaxAnisotropy>;
//    AddressU = wrap;
//    AddressV = wrap;
//};

textureCUBE cubeTex;
samplerCUBE CubeTextureSampler = sampler_state
{
    Texture = <cubeTex>;
    MinFilter = anisotropic;
    MagFilter = anisotropic;
    MipFilter = linear;
	MaxAnisotropy = <kMaxAnisotropy>;
    AddressU = wrap;
    AddressV = wrap;
};

struct VertexShaderInput
{
    float4 Position            : POSITION0;
    float4 normal              : NORMAL0;
    float2 texCoord            : TEXCOORD0;
	float4 color			   : COLOR0;
};

struct VertexShaderOutput
{
    float4 Position            : POSITION0;
    float2 texCoord            : TEXCOORD0;
    float3 worldPos			   : TEXCOORD1;
	float4 color			   : COLOR0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    //float4 worldPosition = mul(float4(input.Position,1), World);
    //float4 viewPosition = mul(worldPosition, View);
    //output.Position = mul(viewPosition, Projection);


    float4 localPosition = mul(input.Position, Local);
    float4 worldPosition = mul(localPosition, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    
    output.texCoord = input.texCoord*waveScale; // waveScale: Controls scale of waves/water. (original value: 5)
    output.worldPos = worldPosition.xyz;

	output.color = input.color;

    return output;
}

// Note: Changing the skybox to a more realistic look will make the water look more realistic (and vice versa)
// Make sure skybox is blurred, though, else it looks bad.
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 sample_1_2 = input.texCoord*0.2 + float2(time,time);
	float2 sample_3_4 = input.texCoord*2 + float2(-time,-time*2);
	float oneMinusTextureLerp = 1-textureLerp;
    
	// 'time' controls direction and rate of water flow
    float4 normalTexture1 = tex2D(NormalTextureSampler, sample_1_2);							// Controls water movement speed (waves layer) : 0.2 default
    float4 normalTexture2 = tex2D(NormalTextureSampler2, sample_1_2);							// Controls water movement speed (waves layer) : 0.2 default
    float4 normalTexture = (textureLerp*normalTexture1)+(oneMinusTextureLerp*normalTexture2);
    float4 normalTexture3 = tex2D(NormalTextureSampler, sample_3_4);							// Controls water movement speed (underwater layer) : -time,-time*2 default
    float4 normalTexture4 = tex2D(NormalTextureSampler2, sample_3_4);							// Controls water movement speed (underwater layer) : -time,-time*2 default
    float4 normalTextureDetail = (textureLerp*normalTexture3)+(oneMinusTextureLerp*normalTexture4);
    
    float3 normal = (((0.5*normalTexture)+(0.5*normalTextureDetail))*2)-1;	// Controls level of detail of waves : 0.5, 0.5 default
    normal = normalize(normal.xzy); // Move from XY plane to XZ plane and normalize
    
    float3 cubeTexCoords = reflect(input.worldPos-EyePos,normal);
    float3 cubeTex = texCUBE(CubeTextureSampler,cubeTexCoords).rgb;
    float4 color = float4((cubeTex*0.95)+(kWaterColor*0.05),1); // Controls how much the skymap influences final color (reflectivity of water)

	color *= oceanAlpha;
	return color;
}

technique Technique1
{
    pass Pass1
    {
		// Transparency: http://rbwhitaker.wikidot.com/transparency-shader
		AlphaBlendEnable = TRUE;
        DestBlend = INVSRCALPHA;
        SrcBlend = ONE; // Use SRCALPHA to set alpha like: color.a *= 0.4; Use ONE for: color *= 0.4

        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
