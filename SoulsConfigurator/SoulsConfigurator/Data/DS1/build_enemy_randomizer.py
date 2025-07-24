"""
Build script to create the Dark Souls Enemy Randomizer executable.
This script should be run to package the enemy randomizer with the SoulsConfigurator.
"""

import os
import sys
import subprocess
import shutil
from pathlib import Path

def build_enemy_randomizer():
    """Build the enemy randomizer executable using pyinstaller"""
    
    # Path to the Dark-Souls-Enemy-Randomizer directory
    randomizer_path = Path("../../../../../Dark-Souls-Enemy-Randomizer")
    
    if not randomizer_path.exists():
        print(f"Error: Dark-Souls-Enemy-Randomizer directory not found at {randomizer_path}")
        print("Please ensure the Dark-Souls-Enemy-Randomizer project is in the correct location.")
        print("Expected path: d:\\git\\Dark-Souls-Enemy-Randomizer")
        return False
    
    # Change to the randomizer directory
    original_cwd = os.getcwd()
    os.chdir(randomizer_path)
    
    try:
        # Install pyinstaller if not present
        print("Installing pyinstaller...")
        subprocess.run([sys.executable, "-m", "pip", "install", "pyinstaller"], check=True)
        
        # Build the executable with proper data files
        print("Building enemy randomizer executable...")
        cmd = [
            sys.executable, 
            "-m", 
            "PyInstaller",
            "--onefile",
            "--console",  # Use console for command line support
            "--name=enemy_randomizer",
            "--icon=favicon.ico",
            "--add-data=favicon.ico;.",
            "--add-data=favicon.png;.",
            "--add-data=eventscripts;eventscripts",
            "randomizer.py"
        ]
        
        subprocess.run(cmd, check=True)
        
        # Copy the executable to the Data/DS1 directory
        exe_path = Path("dist/enemy_randomizer.exe")
        if exe_path.exists():
            destination = Path(original_cwd) / "enemy_randomizer.exe"
            shutil.copy2(exe_path, destination)
            print(f"Successfully copied executable to {destination}")
            
            # Also copy required data files
            data_files = [
                "favicon.ico",
                "favicon.png",
                "enemy_randomizer.ini"  # This will be created separately
            ]
            
            for file in data_files:
                if Path(file).exists():
                    shutil.copy2(file, Path(original_cwd) / file)
                    print(f"Copied {file}")
            
            # Copy eventscripts directory if it exists
            eventscripts_dir = Path("eventscripts")
            if eventscripts_dir.exists():
                dest_eventscripts = Path(original_cwd) / "eventscripts"
                if dest_eventscripts.exists():
                    shutil.rmtree(dest_eventscripts)
                shutil.copytree(eventscripts_dir, dest_eventscripts)
                print("Copied eventscripts directory")
            
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
    print("Building Dark Souls Enemy Randomizer...")
    success = build_enemy_randomizer()
    
    if success:
        print("\nBuild completed successfully!")
        print("The enemy randomizer executable and required files have been copied to the Data/DS1 directory.")
    else:
        print("\nBuild failed. Please check the error messages above.")
        sys.exit(1)
