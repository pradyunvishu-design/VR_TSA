$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Speech
$practiceRoot = 'C:\VR-TSA\New Unity Project\Assets\Practice'
$voiceRoot = Join-Path $practiceRoot 'Resources\PracticeVoice'
New-Item -ItemType Directory -Path $voiceRoot -Force | Out-Null
$source = Get-Content -LiteralPath (Join-Path $practiceRoot 'Scripts\ScenarioCatalog.cs') -Raw
$lines = [ordered]@{
    intro = 'I am Rook, your practice companion. Inspect the patients. Decide who needs help first.'
    diagnose = 'Correct priority. Use the observed signs to identify the condition.'
    priority_wrong = 'Check again. A breathing emergency or uncontrolled bleeding takes priority over a minor injury.'
    diagnosis_wrong = 'That diagnosis does not fit the observed signs. Inspect again.'
    wrong = 'Pause and check your choice, order and placement. Follow the highlighted practice target.'
    complete = 'Practice sequence complete. Review your score. A perfect attempt is required for mastery.'
}
$cases = [regex]::Matches($source, 'instructions=new\[\]\{([^}]+)\}')
for ($caseIndex=0; $caseIndex -lt $cases.Count; $caseIndex++) {
    $phrases = [regex]::Matches($cases[$caseIndex].Groups[1].Value, '"((?:\\.|[^"\\])*)"')
    for ($stepIndex=0; $stepIndex -lt $phrases.Count; $stepIndex++) {
        $lines["case${caseIndex}_${stepIndex}"] = [regex]::Unescape($phrases[$stepIndex].Groups[1].Value)
    }
}
$speaker = New-Object System.Speech.Synthesis.SpeechSynthesizer
$speaker.Rate = 1
try {
    foreach ($entry in $lines.GetEnumerator()) {
        $path = Join-Path $voiceRoot ($entry.Key + '.wav')
        $speaker.SetOutputToWaveFile($path)
        $speaker.Speak($entry.Value)
        $speaker.SetOutputToNull()
    }
} finally { $speaker.Dispose() }
Write-Output "ROBOT_VOICE_FILES=$($lines.Count)"
