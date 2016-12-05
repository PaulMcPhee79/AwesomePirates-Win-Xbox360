float4x4 Local;
float4x4 World;
float4x4 View;
float4x4 Projection;

float metronome;
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

	//output.Position = input.Position;
    output.texCoord = input.texCoord;
	output.color = input.color;
    return output;
}

float4 main(VertexShaderOutput input) : COLOR0
{
    // Look up into the main texture.
	float4 color = tex2D(TextureSampler, input.texCoord) * input.color;
	//float2 adjTexCoord = float2((input.texCoord.x - 0.8754) * 8.46, (input.texCoord.y - 0.5112) * 28.0544);

	//color.rgb += ((sliderX - adjTexCoord.x) % 1);
	//color.rgb += max(0, sliderX - adjTexCoord.x);
	//color.rgb *= tan(abs(metronome));
	//color.rgb += tan(abs(metronome)) * ((1 - sliderX) * (1 - adjTexCoord.x));
	//color.rgb *= tan(metronome);
	//color.rgb *= metronome;

	//color.rgb *= 6 * float3(0.35 * color.r, 0.35 * color.g, 0.95 * color.b) * metronome; // Not bad
	//color.rgb *= 4 * float3(0.5 * color.r, 0.5 * color.g, 0.75 * color.b) * metronome; // Not bad
	//color.rgb *= 2 * color.r * metronome; // Not bad
	//color.rgb *= 4 * float3(color.r / 2, color.g / 2, color.b) * metronome; // Not bad
	//color.rgb *= (1 / ((color.r + color.g) / (2 * color.b))) * metronome;



	color.rgb *= float3(2 * metronome * color.r + color.r / 4, 2 * metronome * color.g + color.g / 4, color.b + 4 * metronome * color.b); // best


	//if (color.a < 0.4)
	//{

		//color.a *= metronome;
		//color = tan(0.8 + 0.7 * metronome) * float4(color.a * 169.0 / 255.0, color.a * 93.0 / 255.0, color.a * 11.0 / 255.0, color.a); // Brown

		//color = tan(0.8 + 0.7 * metronome) * float4(color.a * 49.0 / 255.0, color.a * 200.0 / 255.0, color.a * 170.0 / 255.0, color.a); // Aqua
		//color = tan(0.8 + 0.7 * metronome) * float4(color.a * 234.0 / 255.0, color.a * 154.0 / 255.0, color.a * 0.0 / 255.0, color.a); // Orange
	//}
	//else
	//	color *= 0.7 + 0.6f * metronome;
		//color.a *= metronome; // float3(metronome,metronome,metronome); // * tan(0.8 + 0.6 * metronome);

    return color;
}

technique Technique1
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
