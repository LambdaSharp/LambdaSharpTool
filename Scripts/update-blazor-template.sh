#!/bin/bash
if [ -z "$LAMBDASHARP" ]; then
    echo "ERROR: environment variable \$LAMBDASHARP is not set"
    exit 1
fi
set -x

###
# INSTALLING ZIP ON WINDOWS
###
# 1. Navigate to this sourceforge page: https://sourceforge.net/projects/gnuwin32/files/zip/3.0/
# 2. Download zip-3.0-bin.zip
# 3. In the zipped file, in the bin folder, find the file zip.exe.
# 4. Extract the file zip.exe to your mingw64 bin folder (for me: C:\Program Files\Git\mingw64\bin)
# 5. Navigate to to this sourceforge page: https://sourceforge.net/projects/gnuwin32/files/bzip2/1.0.5/
# 6. Download bzip2-1.0.5-bin.zip
# 7. In the zipped file, in the bin folder, find the file bzip2.dll
# 8. Extract bzip2.dll to your mingw64\bin folder (same folder as above: C:\Program Files\Git\mingw64\bin)

cd $LAMBDASHARP/Resources/

# remove old zip file, just in case
rm BlazorProjectTemplate.zip > /dev/null 2>&1

# zip project without any build artifacts
cd BlazorAppTemplate
rm -rf bin obj > /dev/null 2>&1
zip -r ../BlazorProjectTemplate.zip *

cd ..

# update blazor project in tool
cp BlazorProjectTemplate.zip $LAMBDASHARP/src/LambdaSharp.Tool/Resources/.
rm BlazorProjectTemplate.zip > /dev/null 2>&1
