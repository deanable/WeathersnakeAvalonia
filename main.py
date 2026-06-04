import subprocess, sys, os, time

def build():
    subprocess.run(["dotnet", "build", "-c", "Release"], check=True)

def run():
    exe = os.path.join("bin", "Release", "net10.0", "WeathersnakeAvalonia.exe")
    if os.name == "nt" and os.path.exists(exe):
        proc = subprocess.Popen([exe])
    else:
        proc = subprocess.Popen(["dotnet", "run", "-c", "Release"])
    return proc

if __name__ == "__main__":
    if "--build" in sys.argv:
        build()
    if "--run" in sys.argv:
        proc = run()
        time.sleep(5)
        if proc.poll() is None:
            proc.terminate()
