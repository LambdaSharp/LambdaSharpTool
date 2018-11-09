rm *.nupkg

dotnet publish \
    --configuration Release

dotnet pack \
    --configuration Release \
    --output ./

dotnet tool uninstall \
    --global \
    MindTouch.LambdaSharp.Tool \

dotnet tool install \
    --global \
    --add-source $LAMBDASHARP/src/MindTouch.LambdaSharp.Tool/ \
    MindTouch.LambdaSharp.Tool \
    --version 0.4