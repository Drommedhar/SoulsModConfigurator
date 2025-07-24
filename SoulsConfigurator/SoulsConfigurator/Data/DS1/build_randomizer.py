"""
Build script to create the Dark Souls Item Randomizer executable.
This script should be run to package the randomizer with the SoulsConfigurator.
"""

import os
import sys
import subprocess
import shutil
from pathlib import Path

def build_randomizer():
    """Build the randomizer executable using pyinstaller"""
    
    # Path to the DarkSoulsItemRandomizer directory
    randomizer_path = Path("../../../../../DarkSoulsItemRandomizer")
    
    if not randomizer_path.exists():
        print(f"Error: DarkSoulsItemRandomizer directory not found at {randomizer_path}")
        print("Please ensure the DarkSoulsItemRandomizer project is in the correct location.")
        print("Expected path: d:\\git\\DarkSoulsItemRandomizer")
        return False
    
    # Change to the randomizer directory
    original_cwd = os.getcwd()
    os.chdir(randomizer_path)
    
    try:
        # Install requirements if needed
        print("Installing requirements...")
        subprocess.run([sys.executable, "-m", "pip", "install", "-r", "requirements.txt"], check=True)
        
        # Install pyinstaller if not present
        print("Installing pyinstaller...")
        subprocess.run([sys.executable, "-m", "pip", "install", "pyinstaller"], check=True)
        
        # Build the executable with proper data files
        print("Building randomizer executable...")
        cmd = [
            sys.executable, 
            "-m", 
            "PyInstaller",
            "--onefile",
            "--console",  # Changed from --windowed to --console for command line support
            "--name=randomizer_gui",
            "--icon=favicon.ico",
            "--add-data=favicon.gif;.",
            "--add-data=favicon.ico;.",
            "randomizer_gui.py"
        ]
        
        subprocess.run(cmd, check=True)
        
        # Copy the executable to the Data/DS1 directory
        exe_path = Path("dist/randomizer_gui.exe")
        if exe_path.exists():
            destination = Path(original_cwd) / "randomizer_gui.exe"
            shutil.copy2(exe_path, destination)
            print(f"Successfully copied executable to {destination}")
            
            # Also copy required data files
            data_files = [
                "favicon.gif",
                "favicon.ico",
                "randomizer.ini"
            ]
            
            for file in data_files:
                if Path(file).exists():
                    shutil.copy2(file, Path(original_cwd) / file)
                    print(f"Copied {file}")
            
            return True
        else:
            print("Error: Executable was not created successfully")
            return False
            
    except subprocess.CalledProcessError as e:
        print(f"Error during build process: {e}")
        return False
    except Exception as e:
        print(f"Unexpected error: {e}")
        return False
    finally:
        os.chdir(original_cwd)

if __name__ == "__main__":
    print("Building Dark Souls Item Randomizer...")
    success = build_randomizer()
    
    if success:
        print("\nBuild completed successfully!")
        print("The randomizer executable and required files have been copied to the Data/DS1 directory.")
    else:
        print("\nBuild failed. Please check the error messages above.")
        sys.exit(1)
