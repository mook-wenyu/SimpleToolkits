# 测试配置生成修复
Write-Host "=== 测试配置生成修复 ===" -ForegroundColor Green

# 检查 Excel 文件
Write-Host "`n1. 检查 Excel 文件:" -ForegroundColor Yellow
Get-ChildItem "Assets\ExcelConfigs\*.xlsx" | ForEach-Object {
    Write-Host "  - $($_.Name)" -ForegroundColor Cyan
}

# 检查现有的 CS 文件
Write-Host "`n2. 检查现有的 CS 文件:" -ForegroundColor Yellow
Get-ChildItem "Assets\Scripts\Configs\*.cs" | ForEach-Object {
    Write-Host "  - $($_.Name)" -ForegroundColor Cyan
}

# 检查现有的 JSON 文件
Write-Host "`n3. 检查现有的 JSON 文件:" -ForegroundColor Yellow
if (Test-Path "Assets\GameRes\JsonConfigs") {
    $jsonFiles = Get-ChildItem "Assets\GameRes\JsonConfigs\*.json" -ErrorAction SilentlyContinue
    if ($jsonFiles) {
        $jsonFiles | ForEach-Object {
            Write-Host "  - $($_.Name) (大小: $($_.Length) 字节)" -ForegroundColor Cyan
        }
    } else {
        Write-Host "  没有找到 JSON 文件" -ForegroundColor Red
    }
} else {
    Write-Host "  JSON 目录不存在" -ForegroundColor Red
}

Write-Host "`n=== 修复说明 ===" -ForegroundColor Green
Write-Host "主要修复内容:" -ForegroundColor White
Write-Host "1. 将 ExcelConfig.ConfigName 拆分为 ClassName 和 JsonName" -ForegroundColor White
Write-Host "2. ClassName 用于类型查找 (如: 'Example')" -ForegroundColor White
Write-Host "3. JsonName 用于 JSON 文件命名 (如: 'Example_Config')" -ForegroundColor White
Write-Host "4. 修复了类型查找失败导致 JSON 文件为空的问题" -ForegroundColor White

Write-Host "`n请在 Unity 编辑器中运行 'Simple Toolkits/Excel To Json' 来测试修复效果" -ForegroundColor Yellow
