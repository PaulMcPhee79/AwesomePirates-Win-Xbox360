float4x4 View;
float4x4 Projection;

textureCUBE cubeTex;
samplerCUBE CubeTextureSampler = sampler_state
{
    Texture = <cubeTex>;
    MinFilter = linear;
    MagFilter = linear;
    MipFilter = linear;
    AddressU = wrap;
    AddressV = wrap;
};

struct VS_INPUT
{
    float3 position            : POSITION0;
};

// output from the vertex shader, and input to the pixel shader.
// lightDirection and viewDirection are in world space.
// NOTE: even though the tangentToWorld matrix is only marked 
// with TEXCOORD3, it will actually take TEXCOORD3, 4, and 5.
struct VS_OUTPUT
{
    float4 position            : POSITION0;
    float3 texCoord            : TEXCOORD0;
};

VS_OUTPUT VertexShaderFunction( VS_INPUT input )
{
    VS_OUTPUT output;
    
    // we multiply by the transform matrices
    // we dont want the sky to move relative 
    // to the camera though, so we only use 
    // the 3x3 version of the view matrix
    output.position = float4(mul(input.position, (float3x3)View),1);
    output.position = mul(output.position, Projection);

    // pass the texture coordinate through without additional processing
    output.texCoord = input.position;
    
    return output;
}

float4 PixelShaderFunction( VS_OUTPUT input ) : COLOR0
{
	// sample from the texture... yes, it's that easy
    return texCUBE(CubeTextureSampler, input.texCoord);
}

Technique technique0
{
    Pass pass0
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}