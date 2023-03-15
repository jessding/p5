# ppp-ml-unity
## IMPORTANT NOTES
 - Use the "python.exe" in the /Assets/StreamingAssets/embedded-python folder in order to perform "./python.exe -m pip install PACKAGE_NAME(S)" in order to install PyPI packages.
 - For each new scene, add the "Managers" Prefab in order to properly initialize the Python runtime.
 - Look up Python.NET for more info on the Python embedding with Unity.
 - PyPI packages installed (the main ones): rdkit, MDanalysis, numpy, PolyPly.

## TODO
 - Add libraries for MacOS and Linux and add proper RuntimeDLL linking in the PythonManager script.
