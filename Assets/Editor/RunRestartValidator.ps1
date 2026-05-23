$body = @{menuPath='Tools/Validate Restart Flow'} | ConvertTo-Json
Invoke-RestMethod -Uri 'http://localhost:8090/skill/editor_execute_menu?mode=auto' -Method POST -ContentType 'application/json' -Body $body
