rm *.nupkg

dotnet publish \
    --configuration Release

dotnet pack \
    --configuration Release \
    --output ./

dotnet tool uninstall \
    --global \
    LambdaSharp.Tool \

dotnet tool install \
    --global \
    --add-source $LAMBDASHARP/src/LambdaSharp.Tool/ \
    LambdaSharp.Tool \
    --version 0.5.0.2