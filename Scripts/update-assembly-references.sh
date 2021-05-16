#!/bin/bash
if [ -z "$LAMBDASHARP" ]; then
    echo "ERROR: environment variable \$LAMBDASHARP is not set"
    exit 1
fi

cd $LAMBDASHARP
regex='PackageReference Include="([^"]*)" Version="([^"]*)"'

# find all C# projects, except those in the Tests/Legacy folder
find . -name "*.csproj" -not -path "./Tests/Legacy/*" | while read proj
do
  while read line
  do
    if [[ $line =~ $regex ]]
    then
      name="${BASH_REMATCH[1]}"
      version="${BASH_REMATCH[2]}"
      if [[ $version != *-* ]]
      then
        dotnet add $proj package $name
      fi
    fi
  done < $proj
done