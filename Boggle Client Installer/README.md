# Building the BoggleClientInstaller

Before building the BoggleClientInstaller, be sure to use the code marked with "USE THIS WHEN BUILDING THE CLIENT INSTALLER" in MainWindow.xaml.cs and comment out the other code. This ensures that the rules and sound files can be located on the target machine. This is necessary because currently the installer is configured to use a slightly different directory structure on the target machine than what is used when running the client within Visual Studio.
