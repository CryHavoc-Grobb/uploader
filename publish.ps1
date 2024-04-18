# publish a framework dependent version
dotnet publish src `
    -c Release `
    --self-contained false `
    -o dist

dotnet publish src `
    -c Release `
    --self-contained true `
    -o dist/portable