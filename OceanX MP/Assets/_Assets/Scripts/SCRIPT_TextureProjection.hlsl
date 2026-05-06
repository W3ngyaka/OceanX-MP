void Unity_RotatePDegrees_float(float2 UV, float2 Center, float Rotation, out float2 Out)
{
    Rotation = Rotation * (3.1415926f / 180.0f);
    UV -= Center;
    float s = sin(Rotation);
    float c = cos(Rotation);
    float2x2 rMatrix = float2x2(c, -s, s, c);
    rMatrix *= 0.5;
    rMatrix += 0.5;
    rMatrix = rMatrix * 2 - 1;
    UV.xy = mul(UV.xy, rMatrix);
    UV += Center;
    Out = UV;
}

void TextureProjection_float
(
    in Texture2D Tex,
    in SamplerState SS,
    in float3 Position,
    in float3 Normal,
    in float Tile,
    in float Blend,
    in float Speed,
    in float Rotation,
    out float4 Out
)
{
    float Speed_UV = _Time.y * Speed;
    float3 Node_UV = Position * Tile;
    float3 Node_Blend = pow(abs(Normal), Blend);
    Node_Blend /= dot(Node_Blend, 1.0);

    float2 UV_X, UV_Y, UV_Z;
    Unity_RotatePDegrees_float(Node_UV.yz, float2(0, 0), Rotation, UV_X);
    Unity_RotatePDegrees_float(Node_UV.xz, float2(0, 0), Rotation, UV_Y);
    Unity_RotatePDegrees_float(Node_UV.xy, float2(0, 0), Rotation, UV_Z);

    float4 Node_X = SAMPLE_TEXTURE2D(Tex, SS, UV_X + Speed_UV);
    float4 Node_Y = SAMPLE_TEXTURE2D(Tex, SS, UV_Y + Speed_UV);
    float4 Node_Z = SAMPLE_TEXTURE2D(Tex, SS, UV_Z + Speed_UV);
    Out = Node_X * Node_Blend.x + Node_Y * Node_Blend.y + Node_Z * Node_Blend.z;
}