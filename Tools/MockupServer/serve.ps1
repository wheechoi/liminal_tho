# 목업·위키 HTML을 크롬에서 웹 주소(http://localhost:8765/...)로 열기 위한 작은 로컬 웹서버.
# 이 PC 안에서만 접속됨. Docs 폴더만 읽기 전용으로 보여줌.
#   실행: powershell -ExecutionPolicy Bypass -File Tools\MockupServer\serve.ps1
#   목업: http://localhost:8765/SurplusPerson/SP_1-1_mockup.html
param([int]$Port = 8765)

$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\Docs'))
$types = @{
  '.html' = 'text/html; charset=utf-8'; '.htm' = 'text/html; charset=utf-8'
  '.js' = 'text/javascript; charset=utf-8'; '.css' = 'text/css; charset=utf-8'
  '.json' = 'application/json; charset=utf-8'; '.md' = 'text/markdown; charset=utf-8'
  '.txt' = 'text/plain; charset=utf-8'; '.svg' = 'image/svg+xml'
  '.png' = 'image/png'; '.jpg' = 'image/jpeg'; '.jpeg' = 'image/jpeg'; '.webp' = 'image/webp'; '.gif' = 'image/gif'
}

$listener = New-Object Net.HttpListener
$listener.Prefixes.Add("http://localhost:$Port/")
$listener.Start()
Write-Host "목업 서버 켜짐: http://localhost:$Port/  (폴더: $root)"

while ($listener.IsListening) {
  $ctx = $listener.GetContext()
  $res = $ctx.Response
  try {
    $rel = [Uri]::UnescapeDataString($ctx.Request.Url.AbsolutePath).TrimStart('/')
    $path = [IO.Path]::GetFullPath((Join-Path $root $rel))
    # Docs 폴더 밖은 안 보여줌
    if ($path.StartsWith($root, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path $path -PathType Leaf)) {
      $ext = [IO.Path]::GetExtension($path).ToLower()
      $res.ContentType = if ($types.ContainsKey($ext)) { $types[$ext] } else { 'application/octet-stream' }
      $res.Headers.Add('Cache-Control', 'no-store')  # 고친 내용이 새로고침하면 바로 보이게
      $bytes = [IO.File]::ReadAllBytes($path)
      $res.ContentLength64 = $bytes.Length
      $res.OutputStream.Write($bytes, 0, $bytes.Length)
    } else {
      $res.StatusCode = 404
    }
  } catch {
    $res.StatusCode = 500
  } finally {
    $res.Close()
  }
}
