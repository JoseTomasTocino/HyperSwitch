# HyperSwitch

Pequeña utilidad para Windows 11 que alterna entre:

- **Modo WSL**: inicia el hipervisor de Windows (`hypervisorlaunchtype Auto`) para WSL 2, Docker y Hyper-V.
- **Modo VM**: no inicia el hipervisor de Windows (`hypervisorlaunchtype Off`) para hipervisores que necesitan acceso directo a la virtualización.

La aplicación consulta el estado actual sin elevación. Solo solicita permisos de administrador al aplicar el cambio. Después de que BCDEdit confirme la operación, permite elegir entre reiniciar inmediatamente o hacerlo más tarde.

## Funcionamiento

1. HyperSwitch consulta `Win32_ComputerSystem.HypervisorPresent` para detectar si el hipervisor está activo en la sesión actual.
2. El usuario selecciona el modo deseado y pulsa **Aplicar**.
3. Windows solicita elevación mediante UAC para modificar la entrada de arranque actual.
4. Una vez guardado el cambio, HyperSwitch pregunta si se desea reiniciar en ese momento.

Si se elige reiniciar más tarde, el cambio queda pendiente y no afecta a la sesión actual. Es posible volver a seleccionar el modo original y aplicarlo para cancelar ese cambio antes de reiniciar.

## Compilar

Requiere el SDK de .NET 10:

```powershell
dotnet build -c Release
```

## Publicar un único ejecutable autocontenido

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

El resultado queda en `bin\Release\net10.0-windows\win-x64\publish\HyperSwitch.exe`.

> Importante: **Aplicar** modifica la configuración de arranque mediante BCDEdit. El modo seleccionado no entra en vigor hasta reiniciar Windows.
