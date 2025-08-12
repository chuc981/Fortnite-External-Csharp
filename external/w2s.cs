public static class GameUtils
{
    
    public static Vector3 GetEntityBone(ulong skeletalMesh, int boneIndex)
    {
        ulong boneArray = Driver.Read<ulong>(skeletalMesh + 0x5E8);
        if (boneArray == 0)
        {
            boneArray = Driver.Read<ulong>(skeletalMesh + 0x5E8 + 0x10);
        }
        if (boneArray == 0)
        {
            return new Vector3(0, 0, 0);
        }

        FTransform bone = Driver.Read<FTransform>(boneArray + (ulong)(boneIndex * 0x60));
        FTransform componentToWorld = Driver.Read<FTransform>(skeletalMesh + 0x1E0);
        D3DMatrix matrix = bone.ToMatrixWithScale() * componentToWorld.ToMatrixWithScale();
        return new Vector3(matrix._41, matrix._42, matrix._43);
    }
}
public struct CameraViewPoint
{
    public Vector3 Location;
    public Vector3 Rotation;
    public double Fov;
}


public static class Projection
{
    // --- Constantes de Offset ---
    // TODO: Você DEVE definir estes offsets com os valores corretos para o seu alvo.
    private const ulong LOCATION_POINTER = 0x170; // Substitua pelo offset correto
    private const ulong ROTATION_POINTER = 0x180; // Substitua pelo offset correto
    private const ulong FOV_OFFSET = 0x3AC;       // Offset do FOV no PlayerController

    // --- Configurações da Tela ---
    // TODO: Estes valores devem ser atualizados com a resolução real da sua tela ou janela do jogo.
    public static int WidthMonitor { get; set; } = 1920;
    public static int HeightMonitor { get; set; } = 1080;
    public static double ScreenCenterX => WidthMonitor / 2.0;
    public static double ScreenCenterY => HeightMonitor / 2.0;

      public static CameraViewPoint GetViewPoint(ulong uworldBaseAddress, ulong playerControllerAddress)
    {
        var viewPoint = new CameraViewPoint();

        // Lê os ponteiros para a localização e rotação da câmera.
        ulong locationPointer = Driver.Read<ulong>(uworldBaseAddress + LOCATION_POINTER);
        ulong rotationPointer = Driver.Read<ulong>(uworldBaseAddress + ROTATION_POINTER);

        // Lê os componentes individuais que formam a rotação da câmera.
        double fnrot_a = Driver.Read<double>(rotationPointer);
        double fnrot_b = Driver.Read<double>(rotationPointer + 0x20);
        double fnrot_c = Driver.Read<double>(rotationPointer + 0x1D0);

        // Lê a localização 3D da câmera.
        viewPoint.Location = Driver.Read<Vector3>(locationPointer);

        // Calcula os ângulos de rotação (pitch e yaw) em graus.
        viewPoint.Rotation = new Vector3
        {
            X = Math.Asin(fnrot_c) * (180.0 / Math.PI),
            Y = Math.Atan2(-fnrot_a, fnrot_b) * (180.0 / Math.PI),
            Z = 0
        };

        // Lê o campo de visão (FOV) do PlayerController e o ajusta.
        float rawFov = Driver.Read<float>(playerControllerAddress + FOV_OFFSET);
        viewPoint.Fov = rawFov * 90.0f;

        return viewPoint;
    }
    public static Vector2 ProjectWorldToScreen(Vector3 worldLocation, CameraViewPoint viewPoint)
    {
        D3DMatrix tempMatrix = D3DMatrix.ToMatrix(viewPoint.Rotation, new Vector3(0, 0, 0));

        // 2. Extrai os vetores de eixo da matriz de rotação.
        Vector3 vAxisX = new Vector3(tempMatrix._11, tempMatrix._12, tempMatrix._13);
        Vector3 vAxisY = new Vector3(tempMatrix._21, tempMatrix._22, tempMatrix._23);
        Vector3 vAxisZ = new Vector3(tempMatrix._31, tempMatrix._32, tempMatrix._33);

        // 3. Calcula a posição do ponto em relação à câmera.
        Vector3 vDelta = worldLocation - viewPoint.Location;
        Vector3 vTransformed = new Vector3(
            vDelta.Dot(vAxisY),
            vDelta.Dot(vAxisZ),
            vDelta.Dot(vAxisX)
        );

        // 4. Se o ponto estiver atrás da câmera, ajusta sua profundidade para 1.
        if (vTransformed.Z < 1.0)
        {
            vTransformed.Z = 1.0;
        }

        // 5. Calcula a escala da projeção com base no FOV e na proporção da tela.
        double currentAspectRatio = (double)WidthMonitor / HeightMonitor;
        const double referenceAspectRatio = 16.0 / 9.0;

        double fovHorizontalRad = viewPoint.Fov * (Math.PI / 180.0);
        double fovVerticalRad = 2.0 * Math.Atan(Math.Tan(fovHorizontalRad * 0.5) / referenceAspectRatio);

        double scaleX = 1.0 / Math.Tan(fovHorizontalRad * 0.5);
        double scaleY = 1.0 / Math.Tan(fovVerticalRad * 0.5);

        scaleX *= (referenceAspectRatio / currentAspectRatio);

        // 6. Calcula as coordenadas finais na tela.
        double screenX = ScreenCenterX + (vTransformed.X * scaleX / vTransformed.Z) * ScreenCenterX;
        double screenY = ScreenCenterY - (vTransformed.Y * scaleY / vTransformed.Z) * ScreenCenterY;

        return new Vector2(screenX, screenY);
    }
}