# Gera book.babel traduzido e instala no jogo. O jogo original (guest) le o book.babel do disco
# no boot e nao ve o .f2mod, entao menus, legendas e missoes so ficam em PT com o arquivo trocado.
# Base SEMPRE o original guardado em book.babel.original (copiado do jogo na primeira vez).
param([Parameter(Mandatory=$true)][string]$Game, [switch]$DryRun, [switch]$SemAcento)
$W = $PSScriptRoot
$Target = "$Game\assets\game\data\language\en-uk\text\book.babel"
$Orig = "$W\book.babel.original"
$utf8 = New-Object Text.UTF8Encoding($false)

if (-not (Test-Path $Orig)) {
  if ((Get-Item $Target).Length -ne 2631447) { throw "book.babel do jogo nao parece o original; restaure antes" }
  Copy-Item $Target $Orig
}
Add-Type -Path "$W\BabelPatch.cs"

# mesmas regras do build_mod.ps1: le o csv que ele monta (book.babel.csv dentro do mod)
$mod = [IO.File]::ReadAllBytes("$Game\assets\mods\traducao_ptbr.f2mod")
$s = $utf8.GetString($mod); $i = $s.IndexOf("key,value`n")
$ov = New-Object 'Collections.Generic.Dictionary[uint32,string]'
foreach ($l in $s.Substring($i + 10).Split("`n")) {
  if ($l -notmatch '^0x([0-9A-Fa-f]{8}),(.*)$') { continue }
  $v = $Matches[2]
  if ($v.StartsWith('"')) { $v = $v.Substring(1, $v.Length - 2).Replace('""', '"') }
  $sb = New-Object Text.StringBuilder
  for ($j = 0; $j -lt $v.Length; $j++) {
    $c = $v[$j]
    if ($c -eq '\' -and $j + 1 -lt $v.Length) {
      $n = $v[$j + 1]
      switch -CaseSensitive ($n) { 'n' { [void]$sb.Append("`n"); $j++; continue } 't' { [void]$sb.Append("`t"); $j++; continue } 'r' { [void]$sb.Append("`r"); $j++; continue } '\' { [void]$sb.Append('\'); $j++; continue } }
    }
    [void]$sb.Append($c)
  }
  $ov[[Convert]::ToUInt32($Matches[1], 16)] = $sb.ToString()
}
"traducoes lidas do mod: $($ov.Count)"
# -SemAcento: versao de teste com os acentos tirados de tudo (descobrir se o crash do aluguel vem deles)
if ($SemAcento) {
  foreach ($k in @($ov.Keys)) {
    $d = $ov[$k].Normalize([Text.NormalizationForm]::FormD)
    $ov[$k] = (-join ($d.ToCharArray() | Where-Object { [Globalization.CharUnicodeInfo]::GetUnicodeCategory($_) -ne 'NonSpacingMark' })).Normalize([Text.NormalizationForm]::FormC)
  }
}

$err = New-Object 'Collections.Generic.List[string]'
$bytes = [BabelPatch]::Write([IO.File]::ReadAllBytes($Orig), $ov, $err)
$err | Select-Object -First 20
"erros: $($err.Count)  tamanho: $([Math]::Round($bytes.Length / 1MB, 2)) MB"

# confere: reler e comparar com as traducoes
$lg = 0; $tl = $null
$back = [BabelPatch]::Read($bytes, [ref]$lg, [ref]$tl)
$diff = 0; foreach ($k in $ov.Keys) { if ($back[$k] -ne $ov[$k]) { $diff++ } }
"textos no arquivo novo: $($back.Count)  diferentes do esperado: $diff  maior bloco: $lg"
[IO.File]::WriteAllBytes("$W\book.babel.ptbr", $bytes)
if ($DryRun -or $err.Count -or $diff) { return }
Copy-Item "$W\book.babel.ptbr" $Target -Force
"instalado em $Target (original em $Orig)"
