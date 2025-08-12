using System;
using System.Runtime.InteropServices;

// Para D3DMATRIX, que não está no .NET padrão.
// Geralmente associado ao DirectX, mas aqui definido para corresponder ao código C++.
[StructLayout(LayoutKind.Sequential)]
public struct D3DMatrix
{
    public float _11, _12, _13, _14;
    public float _21, _22, _23, _24;
    public float _31, _32, _33, _34;
    public float _41, _42, _43, _44;

    // Converte a função global matrix_multiplication em uma sobrecarga de operador.
    public static D3DMatrix operator *(D3DMatrix m1, D3DMatrix m2)
    {
        D3DMatrix result = new D3DMatrix();
        result._11 = m1._11 * m2._11 + m1._12 * m2._21 + m1._13 * m2._31 + m1._14 * m2._41;
        result._12 = m1._11 * m2._12 + m1._12 * m2._22 + m1._13 * m2._32 + m1._14 * m2._42;
        result._13 = m1._11 * m2._13 + m1._12 * m2._23 + m1._13 * m2._33 + m1._14 * m2._43;
        result._14 = m1._11 * m2._14 + m1._12 * m2._24 + m1._13 * m2._34 + m1._14 * m2._44;
        result._21 = m1._21 * m2._11 + m1._22 * m2._21 + m1._23 * m2._31 + m1._24 * m2._41;
        result._22 = m1._21 * m2._12 + m1._22 * m2._22 + m1._23 * m2._32 + m1._24 * m2._42;
        result._23 = m1._21 * m2._13 + m1._22 * m2._23 + m1._23 * m2._33 + m1._24 * m2._43;
        result._24 = m1._21 * m2._14 + m1._22 * m2._24 + m1._23 * m2._34 + m1._24 * m2._44;
        result._31 = m1._31 * m2._11 + m1._32 * m2._21 + m1._33 * m2._31 + m1._34 * m2._41;
        result._32 = m1._31 * m2._12 + m1._32 * m2._22 + m1._33 * m2._32 + m1._34 * m2._42;
        result._33 = m1._31 * m2._13 + m1._32 * m2._23 + m1._33 * m2._33 + m1._34 * m2._43;
        result._34 = m1._31 * m2._14 + m1._32 * m2._24 + m1._33 * m2._34 + m1._34 * m2._44;
        result._41 = m1._41 * m2._11 + m1._42 * m2._21 + m1._43 * m2._31 + m1._44 * m2._41;
        result._42 = m1._41 * m2._12 + m1._42 * m2._22 + m1._43 * m2._32 + m1._44 * m2._42;
        result._43 = m1._41 * m2._13 + m1._42 * m2._23 + m1._43 * m2._33 + m1._44 * m2._43;
        result._44 = m1._41 * m2._14 + m1._42 * m2._24 + m1._43 * m2._34 + m1._44 * m2._44;
        return result;
    }

    // Converte a função global to_matrix
    public static D3DMatrix ToMatrix(Vector3 rot, Vector3 origin)
    {
        double radPitch = (rot.X * Math.PI / 180);
        double radYaw = (rot.Y * Math.PI / 180);
        double radRoll = (rot.Z * Math.PI / 180);

        double sp = Math.Sin(radPitch);
        double cp = Math.Cos(radPitch);
        double sy = Math.Sin(radYaw);
        double cy = Math.Cos(radYaw);
        double sr = Math.Sin(radRoll);
        double cr = Math.Cos(radRoll);

        D3DMatrix matrix = new D3DMatrix();
        matrix._11 = (float)(cp * cy);
        matrix._12 = (float)(cp * sy);
        matrix._13 = (float)(sp);
        matrix._14 = 0f;
        matrix._21 = (float)(sr * sp * cy - cr * sy);
        matrix._22 = (float)(sr * sp * sy + cr * cy);
        matrix._23 = (float)(-sr * cp);
        matrix._24 = 0f;
        matrix._31 = (float)(-(cr * sp * cy + sr * sy));
        matrix._32 = (float)(cy * sr - cr * sp * sy);
        matrix._33 = (float)(cr * cp);
        matrix._34 = 0f;
        matrix._41 = (float)origin.X;
        matrix._42 = (float)origin.Y;
        matrix._43 = (float)origin.Z;
        matrix._44 = 1f;
        return matrix;
    }
}


[StructLayout(LayoutKind.Sequential)]
public struct Vector2
{
    public double X;
    public double Y;

    public Vector2(double x, double y)
    {
        X = x;
        Y = y;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct Vector3
{
    public double X;
    public double Y;
    public double Z;

    public Vector3(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public double Dot(Vector3 v)
    {
        return X * v.X + Y * v.Y + Z * v.Z;
    }

    public double Distance(Vector3 v)
    {
        double dx = v.X - X;
        double dy = v.Y - Y;
        double dz = v.Z - Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public Vector3 Normalized()
    {
        double mag = Magnitude();
        return new Vector3(X / mag, Y / mag, Z / mag);
    }

    public double Magnitude()
    {
        return Math.Sqrt(X * X + Y * Y + Z * Z);
    }

    public static Vector3 operator +(Vector3 a, Vector3 b)
    {
        return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    public static Vector3 operator -(Vector3 a, Vector3 b)
    {
        return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    public static Vector3 operator *(Vector3 a, double scalar)
    {
        return new Vector3(a.X * scalar, a.Y * scalar, a.Z * scalar);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct FQuat
{
    public double X;
    public double Y;
    public double Z;
    public double W;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct FTransform
{
    public FQuat Rot;
    public Vector3 Translation;
    // pad[4] foi omitido, o Pack = 4 e o layout sequencial devem lidar com o alinhamento,
    // ou pode ser substituído por um campo privado `int _padding0` se for necessário.
    public Vector3 Scale; // Renomeado de 'scal2e' para clareza
    // pad1[4] foi omitido.

    public D3DMatrix ToMatrixWithScale()
    {
        // Garante que a escala não seja zero
        Vector3 s = new Vector3(
            Scale.X == 0.0 ? 1.0 : Scale.X,
            Scale.Y == 0.0 ? 1.0 : Scale.Y,
            Scale.Z == 0.0 ? 1.0 : Scale.Z
        );

        var m = new D3DMatrix
        {
            _41 = (float)Translation.X,
            _42 = (float)Translation.Y,
            _43 = (float)Translation.Z
        };

        double x2 = Rot.X + Rot.X;
        double y2 = Rot.Y + Rot.Y;
        double z2 = Rot.Z + Rot.Z;

        double xx2 = Rot.X * x2;
        double yy2 = Rot.Y * y2;
        double zz2 = Rot.Z * z2;
        m._11 = (float)((1.0f - (yy2 + zz2)) * s.X);
        m._22 = (float)((1.0f - (xx2 + zz2)) * s.Y);
        m._33 = (float)((1.0f - (xx2 + yy2)) * s.Z);

        double yz2 = Rot.Y * z2;
        double wx2 = Rot.W * x2;
        m._32 = (float)((yz2 - wx2) * s.Z);
        m._23 = (float)((yz2 + wx2) * s.Y);

        double xy2 = Rot.X * y2;
        double wz2 = Rot.W * z2;
        m._21 = (float)((xy2 - wz2) * s.Y);
        m._12 = (float)((xy2 + wz2) * s.X);

        double xz2 = Rot.X * z2;
        double wy2 = Rot.W * y2;
        m._31 = (float)((xz2 + wy2) * s.Z);
        m._13 = (float)((xz2 - wy2) * s.X);

        m._14 = 0.0f;
        m._24 = 0.0f;
        m._34 = 0.0f;
        m._44 = 1.0f;
        return m;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct FPlane
{
    public double X;
    public double Y;
    public double Z;
    public double W;
}

// Em C#, não podemos ter um array de tamanho fixo e campos sobrepostos da mesma forma
// que uma união C++ sem usar `unsafe` ou `FieldOffset`. `FieldOffset` é a abordagem segura.
[StructLayout(LayoutKind.Explicit)]
public struct FMatrix
{
    [FieldOffset(0)]
    public FPlane XPlane;

    [FieldOffset(32)] // 4 * 8 bytes
    public FPlane YPlane;

    [FieldOffset(64)]
    public FPlane ZPlane;

    [FieldOffset(96)]
    public FPlane WPlane;

    // O array m[4][4] está efetivamente sobreposto a estes planos.
    // Acessar via `m` requer métodos getter/setter ou código `unsafe`.
    // O construtor é a forma mais direta de interagir.

    public FMatrix(FPlane xPlane, FPlane yPlane, FPlane zPlane, FPlane wPlane)
    {
        XPlane = xPlane;
        YPlane = yPlane;
        ZPlane = zPlane;
        WPlane = wPlane;
    }
}