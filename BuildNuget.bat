"C:\Program Files (x86)\NuGet3\nuget.exe" pack NuGetPackages/MonoGame.Framework.WindowsDX.nuspec		-OutputDirectory NuGetPackages\Output\  -BasePath .  -Version 3.8.9009.0  -Properties Configuration=Release
"C:\Program Files (x86)\NuGet3\nuget.exe" pack NuGetPackages/MonoGame.Framework.DesktopGL.nuspec		-OutputDirectory NuGetPackages\Output\  -BasePath .  -Version 3.8.9009.0  -Properties Configuration=Release
"C:\Program Files (x86)\NuGet3\nuget.exe" pack NuGetPackages/MonoGame.Framework.Android.nuspec			-OutputDirectory NuGetPackages\Output\  -BasePath .  -Version 3.8.9009.0  -Properties Configuration=Release
"C:\Program Files (x86)\NuGet3\nuget.exe" pack NuGetPackages/MonoGame.Framework.MacOS.nuspec			-OutputDirectory NuGetPackages\Output\  -BasePath .  -Version 3.8.9009.0  -Properties Configuration=Release
"C:\Program Files (x86)\NuGet3\nuget.exe" pack NuGetPackages/MonoGame.Framework.iOS.nuspec				-OutputDirectory NuGetPackages\Output\  -BasePath .  -Version 3.8.9009.0  -Properties Configuration=Release
"C:\Program Files (x86)\NuGet3\nuget.exe" pack NuGetPackages/MonoGame.Framework.WindowsUniversal.nuspec	-OutputDirectory NuGetPackages\Output\  -BasePath .  -Version 3.8.9009.0  -Properties Configuration=Release

"C:\Program Files (x86)\NuGet3\nuget.exe" pack NuGetPackages/MonoGame.Framework.Portable.nuspec			-OutputDirectory NuGetPackages\Output\  -BasePath .  -Version 3.8.9009.0  -Properties Configuration=Release
"C:\Program Files (x86)\NuGet3\nuget.exe" pack NuGetPackages/MonoGame.Framework.Content.Pipeline.Portable.nuspec -OutputDirectory NuGetPackages\Output\  -BasePath .  -Version 3.8.9009.0  -Properties Configuration=Release

"C:\Program Files (x86)\NuGet3\nuget.exe" pack NuGetPackages/MonoGame.Framework.Cardboard.nuspec        -OutputDirectory NuGetPackages\Output\  -BasePath .  -Version 3.8.9009.0  -Properties Configuration=Release

@pause