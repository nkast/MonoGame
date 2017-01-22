//-----------------------------------------------------------------------------
// SpriteEffect.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

#include "Macros.fxh"


DECLARE_TEXTURE(Texture , 0);
DECLARE_TEXTURE(Texture1, 1);
DECLARE_TEXTURE(Texture2, 2);
DECLARE_TEXTURE(Texture3, 3);
DECLARE_TEXTURE(Texture4, 4);
DECLARE_TEXTURE(Texture5, 5);
DECLARE_TEXTURE(Texture6, 6);
DECLARE_TEXTURE(Texture7, 7);

BEGIN_CONSTANTS
MATRIX_CONSTANTS

    float4x4 MatrixTransform    _vs(c0) _cb(c0);

END_CONSTANTS


struct VSOutput
{
	float4 position		: SV_Position;
	float4 color		: COLOR0;
    float2 texCoord		: TEXCOORD0;
};

struct VSOutput2
{
	float4 position     : SV_Position;
	float4 color        : COLOR0;
	float3 texCoord     : TEXCOORD0;
};

VSOutput SpriteVertexShader(	float4 position	: POSITION0,
								float4 color	: COLOR0,
								float2 texCoord	: TEXCOORD0)
{
	VSOutput output;
    output.position = mul(position, MatrixTransform);
	output.color = color;
	output.texCoord = texCoord;
	return output;
}


float4 SpritePixelShader(VSOutput input) : SV_Target0
{
    return SAMPLE_TEXTURE(Texture, input.texCoord) * input.color;
}


VSOutput2 SpriteVertexShader2(float4 position	: POSITION0,
	float4 color : COLOR0,
	float3 texCoord : TEXCOORD0)
{
	VSOutput2 output;
	output.position = mul(position, MatrixTransform);
	output.color = color;
	output.texCoord = texCoord;
	return output;
}


float4 SpritePixelShader2(VSOutput2 input) : SV_Target0
{
	if (input.texCoord.z <= 3)
	{
		if (input.texCoord.z <= 1)
		{
			if (input.texCoord.z == 0)
				return SAMPLE_TEXTURE(Texture, input.texCoord.xy) * input.color;
			else
				return SAMPLE_TEXTURE(Texture1, input.texCoord.xy) * input.color;
		}
		else
		{
			if (input.texCoord.z == 2)
				return SAMPLE_TEXTURE(Texture2, input.texCoord.xy) * input.color;
			else
				return SAMPLE_TEXTURE(Texture3, input.texCoord.xy) * input.color;
		}
	}
	else
	{
		if (input.texCoord.z <= 5)
		{
			if (input.texCoord.z == 4)
				return SAMPLE_TEXTURE(Texture4, input.texCoord.xy) * input.color;
			else
				return SAMPLE_TEXTURE(Texture5, input.texCoord.xy) * input.color;
		}
		else
		{
			if (input.texCoord.z == 6)
				return SAMPLE_TEXTURE(Texture6, input.texCoord.xy) * input.color;
			else
				return SAMPLE_TEXTURE(Texture7, input.texCoord.xy) * input.color;
		}
	}	
}


//TECHNIQUE( SpriteBatch, SpriteVertexShader, SpritePixelShader );
TECHNIQUE(SpriteBatch2, SpriteVertexShader2, SpritePixelShader2);
