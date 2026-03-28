# -*- coding: utf-8 -*-
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$token = '$env:NOTION_TOKEN'
$headers = @{'Authorization'="Bearer $token"; 'Notion-Version'='2022-06-28'; 'Content-Type'='application/json; charset=utf-8'}
$kiriePageId = '32b45ad0-1ef0-81be-bdc1-c48646692738'
$scenarioDir = 'f:/Claude/KirieSaki/Assets/StreamingAssets/Scenarios'

$charNames = @{
    'kirie'     = [System.Text.Encoding]::UTF8.GetString([byte[]](233,128,134,231,159,159,227,130,173,227,131,170,227,130,168))
    'saki'      = [System.Text.Encoding]::UTF8.GetString([byte[]](229,133,165,230,178,159,227,130,181,227,130,173))
    'suzumura'  = [System.Text.Encoding]::UTF8.GetString([byte[]](233,148,180,230,157,145))
    'hakushoku' = [System.Text.Encoding]::UTF8.GetString([byte[]](231,153,189,232,9128))
}

function Make-TextBlock($text, $blockType='paragraph') {
    if ($text.Length -gt 1990) { $text = $text.Substring(0, 1990) }
    $block = @{ type = $blockType }
    $block[$blockType] = @{ rich_text = @(@{ type='text'; text=@{ content=$text } }) }
    return $block
}

function Upload-Blocks($pageId, $blocks) {
    $i = 0
    while ($i -lt $blocks.Count) {
        $end = [Math]::Min($i+99, $blocks.Count-1)
        $chunk = @($blocks[$i..$end])
        $body = @{ children = $chunk } | ConvertTo-Json -Depth 10 -Compress
        $bodyBytes = [System.Text.Encoding]::UTF8.GetBytes($body)
        Invoke-WebRequest -Uri "https://api.notion.com/v1/blocks/$pageId/children" -Method PATCH -Headers $headers -Body $bodyBytes -UseBasicParsing | Out-Null
        $i += 100
        Start-Sleep -Milliseconds 500
    }
}

$chapters = Get-ChildItem $scenarioDir -Filter 'chapter*.json' | Sort-Object Name
foreach ($file in $chapters) {
    $raw = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    $data = $raw | ConvertFrom-Json

    $subBody = @{
        parent = @{ page_id = $kiriePageId }
        properties = @{ title = @{ title = @(@{ text = @{ content = $data.title } }) } }
    } | ConvertTo-Json -Depth 6 -Compress
    $subBodyBytes = [System.Text.Encoding]::UTF8.GetBytes($subBody)

    $subPage = Invoke-WebRequest -Uri 'https://api.notion.com/v1/pages' -Method POST -Headers $headers -Body $subBodyBytes -UseBasicParsing | Select-Object -ExpandProperty Content | ConvertFrom-Json
    $subId = $subPage.id

    $blocks = @()
    foreach ($cmd in $data.commands) {
        if ($cmd.cmd -eq 'text') {
            if ($cmd.char -eq 'narrator') {
                $blocks += Make-TextBlock $cmd.body 'paragraph'
            } else {
                $name = if ($charNames.ContainsKey($cmd.char)) { $charNames[$cmd.char] } else { $cmd.char }
                $kagi_open  = [System.Text.Encoding]::UTF8.GetString([byte[]](227,128,140))
                $kagi_close = [System.Text.Encoding]::UTF8.GetString([byte[]](227,128,141))
                $line = $name + $kagi_open + $cmd.body + $kagi_close
                $blocks += Make-TextBlock $line 'paragraph'
            }
        }
    }

    if ($blocks.Count -gt 0) {
        Upload-Blocks $subId $blocks
    }
    Write-Output "Done: $($data.title)"
    Start-Sleep -Milliseconds 300
}
Write-Output 'KirieSaki upload complete'
