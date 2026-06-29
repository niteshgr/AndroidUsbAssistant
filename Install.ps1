# Android USB Assistant Setup Wizard
# Visual WPF-based installation script.

Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Xaml
Add-Type -AssemblyName System.Windows.Forms

[xml]$xaml = @"
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Android USB Assistant - Setup"
        Width="480" Height="320"
        WindowStartupLocation="CenterScreen"
        ResizeMode="NoResize"
        Background="#191919"
        BorderBrush="#2D2D2D" BorderThickness="1">
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Title -->
        <TextBlock Grid.Row="0" Text="Android USB Assistant Setup"
                   FontSize="18" FontWeight="Bold" Foreground="#009688"
                   Margin="0,0,0,15"/>

        <!-- Welcome Panel -->
        <StackPanel x:Name="pnlWelcome" Grid.Row="1" Visibility="Visible">
            <TextBlock Text="Welcome to the Android USB Assistant Setup Wizard."
                       Foreground="#E6E6E6" FontSize="11" Margin="0,0,0,10" TextWrapping="Wrap"/>
            <TextBlock Text="This utility installs the system tray application that detects Android USB connection events and enables automated RNDIS USB Tethering."
                       Foreground="#A9A9A9" FontSize="10" Margin="0,0,0,15" TextWrapping="Wrap"/>
            <TextBlock Text="Click Next to customize installation preferences."
                       Foreground="#E6E6E6" FontSize="10"/>
        </StackPanel>

        <!-- Options Panel -->
        <StackPanel x:Name="pnlOptions" Grid.Row="1" Visibility="Collapsed">
            <TextBlock Text="Destination Folder:" Foreground="#E6E6E6" FontSize="10.5" Margin="0,0,0,5"/>
            <Grid Margin="0,0,0,15">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <TextBox x:Name="txtDestPath" Grid.Column="0" IsReadOnly="True"
                         Background="#252526" Foreground="#F0F0F0" BorderBrush="#3F3F46" BorderThickness="1"
                         Padding="5,2" FontSize="10.5"/>
            </Grid>

            <CheckBox x:Name="chkDesktopShortcut" Content="Create a Desktop shortcut" IsChecked="True"
                      Foreground="#E6E6E6" Margin="0,0,0,10" FontSize="10.5"/>
            <CheckBox x:Name="chkStartOnBoot" Content="Start application automatically on logon" IsChecked="True"
                      Foreground="#E6E6E6" Margin="0,0,0,10" FontSize="10.5"/>
        </StackPanel>

        <!-- Progress Panel -->
        <StackPanel x:Name="pnlProgress" Grid.Row="1" Visibility="Collapsed" VerticalAlignment="Center">
            <TextBlock x:Name="lblStatus" Text="Building application (generating single-file binary)..." Foreground="#E6E6E6" FontSize="11" Margin="0,0,0,10"/>
            <ProgressBar x:Name="prgBar" Height="15" IsIndeterminate="True"
                         Background="#2D2D30" BorderThickness="0" Foreground="#009688"/>
        </StackPanel>

        <!-- Complete Panel -->
        <StackPanel x:Name="pnlComplete" Grid.Row="1" Visibility="Collapsed">
            <TextBlock Text="Installation Completed Successfully!"
                       Foreground="#009688" FontSize="13" FontWeight="Bold" Margin="0,0,0,10"/>
            <TextBlock Text="Android USB Assistant has been installed on your PC."
                       Foreground="#A9A9A9" FontSize="10.5" Margin="0,0,0,20"/>
            <CheckBox x:Name="chkLaunchNow" Content="Launch Android USB Assistant now" IsChecked="True"
                      Foreground="#E6E6E6" Margin="0,0,0,10" FontSize="10.5"/>
        </StackPanel>

        <!-- Control Buttons -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,15,0,0">
            <Button x:Name="btnBack" Content="Back" Width="75" Height="28" Margin="0,0,10,0"
                    Background="#2D2D30" Foreground="White" BorderThickness="0" Visibility="Collapsed"/>
            <Button x:Name="btnNext" Content="Next" Width="75" Height="28" Margin="0,0,10,0"
                    Background="#009688" Foreground="White" BorderThickness="0"/>
            <Button x:Name="btnCancel" Content="Cancel" Width="75" Height="28"
                    Background="#2D2D30" Foreground="White" BorderThickness="0"/>
        </StackPanel>
    </Grid>
</Window>
"@

$xamlReader = New-Object System.Xml.XmlNodeReader $xaml
$Window = [Windows.Markup.XamlReader]::Load($xamlReader)

# Get controls from XAML
$pnlWelcome = $Window.FindName("pnlWelcome")
$pnlOptions = $Window.FindName("pnlOptions")
$pnlProgress = $Window.FindName("pnlProgress")
$pnlComplete = $Window.FindName("pnlComplete")

$txtDestPath = $Window.FindName("txtDestPath")
$chkDesktopShortcut = $Window.FindName("chkDesktopShortcut")
$chkStartOnBoot = $Window.FindName("chkStartOnBoot")
$chkLaunchNow = $Window.FindName("chkLaunchNow")

$lblStatus = $Window.FindName("lblStatus")
$prgBar = $Window.FindName("prgBar")

$btnBack = $Window.FindName("btnBack")
$btnNext = $Window.FindName("btnNext")
$btnCancel = $Window.FindName("btnCancel")

$script:currentScreen = 1
$txtDestPath.Text = Join-Path $env:USERPROFILE "AppData\Local\Programs\AndroidUsbAssistant"

function UpdateUI() {
    $btnBack.Visibility = "Collapsed"
    $btnNext.Content = "Next"
    $btnCancel.Content = "Cancel"
    $btnNext.Visibility = "Visible"
    $btnCancel.Visibility = "Visible"
    
    $pnlWelcome.Visibility = "Collapsed"
    $pnlOptions.Visibility = "Collapsed"
    $pnlProgress.Visibility = "Collapsed"
    $pnlComplete.Visibility = "Collapsed"
    
    if ($script:currentScreen -eq 1) {
        $pnlWelcome.Visibility = "Visible"
    }
    elseif ($script:currentScreen -eq 2) {
        $pnlOptions.Visibility = "Visible"
        $btnBack.Visibility = "Visible"
        $btnNext.Content = "Install"
    }
    elseif ($script:currentScreen -eq 3) {
        $pnlProgress.Visibility = "Visible"
        $btnCancel.Visibility = "Collapsed"
        $btnBack.Visibility = "Collapsed"
        $btnNext.Visibility = "Collapsed"
    }
    elseif ($script:currentScreen -eq 4) {
        $pnlComplete.Visibility = "Visible"
        $btnNext.Content = "Finish"
        $btnCancel.Visibility = "Collapsed"
    }
}

$worker = New-Object System.ComponentModel.BackgroundWorker
$worker.add_DoWork({
    param($sender, $e)
    
    $args = $e.Argument
    $destPath = $args[0]
    $createShortcut = $args[1]
    $startOnBoot = $args[2]
    
    # 1. Clean previous build folders if any
    $publishFolder = Join-Path $PSScriptRoot "build\publish"
    if (Test-Path $publishFolder) {
        Remove-Item $publishFolder -Recurse -Force | Out-Null
    }
    
    # 2. Compile using dotnet publish
    $projectPath = Join-Path $PSScriptRoot "src\AndroidUsbAssistant.App\AndroidUsbAssistant.App.csproj"
    $publishCmd = "dotnet publish `"$projectPath`" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -o `"$publishFolder`""
    
    # Run the shell command silently and capture errors
    $processInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processInfo.FileName = "powershell.exe"
    $processInfo.Arguments = "-NoProfile -Command `"$publishCmd`""
    $processInfo.RedirectStandardError = $true
    $processInfo.UseShellExecute = $false
    $processInfo.CreateNoWindow = $true
    
    $process = [System.Diagnostics.Process]::Start($processInfo)
    $process.WaitForExit()
    $stdErr = $process.StandardError.ReadToEnd()
    
    if ($process.ExitCode -ne 0) {
        throw "Compilation failed (Exit Code $($process.ExitCode)): $stdErr"
    }
    
    $exeSource = Join-Path $publishFolder "AndroidUsbAssistant.App.exe"
    if (-not (Test-Path $exeSource)) {
        throw "Compilation output not found. Build process failed."
    }
    
    # 3. Create destination directory
    if (-not (Test-Path $destPath)) {
        New-Item -Path $destPath -ItemType Directory -Force | Out-Null
    }
    
    # 4. Copy binary
    $exeDest = Join-Path $destPath "AndroidUsbAssistant.App.exe"
    Copy-Item $exeSource $exeDest -Force
    
    # 5. Copy Uninstaller script
    $uninstallerSource = Join-Path $PSScriptRoot "build\Uninstall.ps1"
    $uninstallerDest = Join-Path $destPath "Uninstall.ps1"
    Copy-Item $uninstallerSource $uninstallerDest -Force
    
    # 6. Create Shortcuts
    $wshell = New-Object -ComObject Wscript.Shell
    if ($createShortcut) {
        $desktopPath = [Environment]::GetFolderPath("Desktop")
        $shortcut = $wshell.CreateShortcut((Join-Path $desktopPath "Android USB Assistant.lnk"))
        $shortcut.TargetPath = $exeDest
        $shortcut.WorkingDirectory = $destPath
        $shortcut.Description = "Android USB Assistant Desktop Entry"
        $shortcut.Save()
    }
    
    $startMenuFolder = Join-Path ([Environment]::GetFolderPath("StartMenu")) "Programs\Android USB Assistant"
    if (-not (Test-Path $startMenuFolder)) {
        New-Item -Path $startMenuFolder -ItemType Directory -Force | Out-Null
    }
    $shortcut = $wshell.CreateShortcut((Join-Path $startMenuFolder "Android USB Assistant.lnk"))
    $shortcut.TargetPath = $exeDest
    $shortcut.WorkingDirectory = $destPath
    $shortcut.Description = "Android USB Assistant"
    $shortcut.Save()
    
    # 7. Add Windows Startup registry key if requested
    if ($startOnBoot) {
        $regPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
        Set-ItemProperty -Path $regPath -Name "AndroidUsbAssistant" -Value "`"$exeDest`"" -Force
    }
})

$worker.add_RunWorkerCompleted({
    param($sender, $e)
    
    if ($e.Error) {
        [System.Windows.MessageBox]::Show("Setup was unable to compile or copy files.`n`nError details: $($e.Error.Message)", "Installation Error", 0, 16)
        $script:currentScreen = 2
        UpdateUI
    } else {
        $script:currentScreen = 4
        UpdateUI
    }
})

# Control Wire-up
$btnNext.Add_Click({
    if ($script:currentScreen -eq 1) {
        $script:currentScreen = 2
        UpdateUI
    }
    elseif ($script:currentScreen -eq 2) {
        $script:currentScreen = 3
        UpdateUI
        
        # Capture variables on dispatcher thread
        $dest = $txtDestPath.Text
        $shortcut = $chkDesktopShortcut.IsChecked
        $startup = $chkStartOnBoot.IsChecked
        
        $worker.RunWorkerAsync(@($dest, $shortcut, $startup))
    }
    elseif ($script:currentScreen -eq 4) {
        if ($chkLaunchNow.IsChecked) {
            $dest = $txtDestPath.Text
            $exeDest = Join-Path $dest "AndroidUsbAssistant.App.exe"
            Start-Process $exeDest -WorkingDirectory $dest
        }
        $Window.Close()
    }
})

$btnBack.Add_Click({
    if ($script:currentScreen -eq 2) {
        $script:currentScreen = 1
        UpdateUI
    }
})

$btnCancel.Add_Click({
    $Window.Close()
})

UpdateUI
$Window.ShowDialog() | Out-Null
