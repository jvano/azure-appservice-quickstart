@echo off
setlocal enabledelayedexpansion

for /d %%d in (*) do (
    if exist "%%d\run.cmd" (
        pushd "%%d"
        echo Running run.cmd in %%d
        call run.cmd
        popd
    )
)

endlocal
