## What is ChromeTabMonitorConsole
ChromeTabMonitorConsole is a c# Console Application that helps you get "devtools" or "remote debugging protocol" performance data, and Chrome console errors, collect in local fileystem and send to an ElasticSearch / Opensearch endpoint.<br>
It allows you to collect data from Chrome or Edge browser

## How to use it
1. Chrome or Edge browser need to be started with the <code>--remote-debugging-port=XXXX</code> parameter (XXXX = tcp port) to listen for devtools/remote debugging protocol connections.<br>
2. You need to configure the <code>settings.ini</code> file to connect to the configured port. <br>
<br>
Example <br>
- start Edge with this command line : <code>"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" --remote-debugging-port=9227</code>
- configure the <code>rdp_url</code> property in <code>settings.ini</code> like this : <code>rdp_url=http://localhost:9227</code>


