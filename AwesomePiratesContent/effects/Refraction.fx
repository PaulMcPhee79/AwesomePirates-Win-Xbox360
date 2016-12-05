
//float4x4 Local;
//float4x4 World;
//float4x4 View;
//float4x4 Projection;
float2 Dimensions;
float2 DisplacementScroll;
float4 DisplacementFactor;
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

    //float4 localPosition = mul(input.Position, Local);
    //float4 worldPosition = mul(localPosition, World);
    //float4 viewPosition = mul(worldPosition, View);
    //output.Position = mul(viewPosition, Projection);

	output.Position = input.Position;
    output.texCoord = input.texCoord;
	output.color = input.color;
    return output;
}

float4 main(VertexShaderOutput input) : COLOR0
{
    // Look up the displacement amount.
    float2 displacement = tex2D(DisplacementSampler, DisplacementScroll + input.texCoord);  // + input.texCoord / 3
    
    // Offset the main texture coordinates.
	input.texCoord.x += displacement * (DisplacementFactor.x * Dimensions.x) - (DisplacementFactor.y * Dimensions.x);
	input.texCoord.y += displacement * (DisplacementFactor.z * Dimensions.y) - (DisplacementFactor.w * Dimensions.y);

	//input.texCoord.x += displacement * (0.2 * Dimensions.x) - (0.15 * Dimensions.x);
	//input.texCoord.y += displacement * (0.2 * Dimensions.y) - (0.15 * Dimensions.y);

    // Look up into the main texture.
    return tex2D(TextureSampler, input.texCoord) * input.color;
}


technique Refraction
{
    pass Pass1
    {
		VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 main();
    }
}
