:; set -eo pipefail
:; SCRIPT_DIR=$(cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd)
:; ${SCRIPT_DIR}/build.sh "$@"
:; exit $?

@ECHO OFF
set NUKE_TELEMETRY_OPTOUT="1"

where powershell >nul 2>&1 && (
    powershell -ExecutionPolicy ByPass -NoProfile -File "%~dp0build.ps1" %*
) || (
    pwsh -ExecutionPolicy ByPass -NoProfile -File "%~dp0build.ps1" %*
)