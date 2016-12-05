
float4x4 Local;
float4x4 World;
float4x4 View;
float4x4 Projection;
float2 Dimensions;
float2 DisplacementScroll;
uniform const int MaxAnisotropy = 8; // Lower for performance gains

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

texture2D displacementTex;
sampler2D DisplacementSampler = sampler_state
{
    Texture = <displacementTex>;
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
    float4 normal              : NORMAL0;
    float2 texCoord            : TEXCOORD0;
	float4 color			   : COLOR0;
};

struct VertexShaderOutput
{
    float4 Position            : POSITION0;
    float2 texCoord            : TEXCOORD0;
	float4 color			   : COLOR0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 localPosition = mul(input.Position, Local);
    float4 worldPosition = mul(localPosition, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    output.texCoord = input.texCoord;
	output.color = input.color;
    return output;
}

float4 main(VertexShaderOutput input) : COLOR0
{
	float4 dispColor = tex2D(DisplacementSampler, DisplacementScroll + sin(20 * input.texCoord));
	float greyscale = dot(dispColor.rgb, float3(0.3, 0.59, 0.11));
	return (tex2D(TextureSampler, input.texCoord) * input.color) / greyscale;

    //return (1.75 * tex2D(TextureSampler, input.texCoord) * input.color) * (tex2D(DisplacementSampler, DisplacementScroll + sin(20 * input.texCoord))); // 20,27,
}


technique Refraction
{
    pass Pass1
    {
		// TODO: set renderstates here.
		//AlphaBlendEnable = TRUE;
        //DestBlend = INVSRCALPHA;
        //SrcBlend = SRCALPHA; // Use SRCALPHA to set alpha like: color.a *= 0.4; Use ONE for: color *= 0.4

		VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 main();
    }
}
