
float4x4 World;
float4x4 View;
float4x4 Projection;
uniform const int MaxAnisotropy = 4; // Lower for performance gains

texture2D tex;
sampler2D TextureSampler = sampler_state
{
    Texture = <tex>;
    MinFilter = anisotropic;
    MagFilter = anisotropic;
    MipFilter = linear;
	MaxAnisotropy = <MaxAnisotropy>;
    AddressU = wrap;
    AddressV = wrap;
};

struct VertexShaderInput
{
    float4 Position            : POSITION0;
    float2 texCoord            : TEXCOORD0;
	float2 shaderCoord         : TEXCOORD1;
	float4 color			   : COLOR0;
};

struct VertexShaderOutput
{
    float4 Position            : POSITION0;
    float2 texCoord            : TEXCOORD0;
	float2 shaderCoord         : TEXCOORD1;
	float4 color			   : COLOR0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);
	output.texCoord = input.texCoord;
	output.shaderCoord = input.shaderCoord;
	output.color = input.color;
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	return tex2D(TextureSampler, input.texCoord) * input.color;
}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
