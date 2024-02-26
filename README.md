## What is ChromeTabMonitorConsole
ChromeTabMonitorConsole is a c# Console Application that helps you get "devtools" or "remote debugging protocol" performance data, Chrome console errors, render performance, etc.<br>
You can collect in local fileystem and/or send to an ElasticSearch / Opensearch endpoint.<br>
It allows you to collect data from remote Chrome or Edge browser in user PCs to analyze website errors or performance degradations.

## Use case
If you're a developer, usually you get information directly from the browser, or implement JS tracking call to monitor how your application is performing.<br>
If you work on complex applications and widespread teams, things can be more difficult.<br>
You may implement RUM (real user-experience monitoring) solutions, including the right component in your application to monitor how it behaves on different PCs, browser, devices, network connections, etc.<br>
<br>
Unfortunately RUMs are not cheap, sometimes you pay "per user" or "per browser" or based on the volume of data you push to the collector, or "by application" too. Let's imagine to have more than 8000 users on PCs and more than 100 applications, constantly used during the day: if you want to monitor something you need to know exactly what to monitor, or "choose" what you want to view. <br>
Another possibility is to go for a PASSIVE RUM : something you install on the PCs and simply measure everything and send it back to a collector : good solution, but definitely __not cheap__.<br>
<br>
To control the costs you may need to:
- select __few__ specific applications or domains, etc
- select __few__ specific typical users by category (eg: normal users, super users, administrators, etc)
<br>
At this point we assumed that you can modify your application, choosing to enable the RUM components, choosing to enable the RUM only for some application and some (key/sample) users, but  __what if you can't choose?__ <br>
<br>
With the ChromeTabMonitorConsole I want to create a standard open source component that capture the metrics directly from the browser using DevTools protocol, enrich with some data and send back to a standard collector platform (ElasticSearch or OpenSearch, at the moment). Then you can implement the dashboard and explore the data as you like.<br>

## What's next
If you read the documentation and get a look on the data I actually get from the browser, you'll start understanding that there is a lot more to explore.<br>
First step is to release something that actually work and maybe useful to other people, than continue to develop.
The target will be a Windows Service application, to be able to run in background.


## How to use it
1. Chrome or Edge browser need to be started with the <code>--remote-debugging-port=XXXX</code> parameter (XXXX = tcp port) to listen for devtools/remote debugging protocol connections.<br>
2. You need to configure the <code>settings.ini</code> file to connect to the configured port.
   
__Example:__ <br>
- start Edge with this command line : <code>"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" --remote-debugging-port=9227</code> <br>
- configure the <code>rdp_url</code> property in <code>settings.ini</code> like this : <code>rdp_url=http://localhost:9227</code> <br>


## What is Chrome DevTools/RemoteDebugging protocol
Please refed to official documentation : https://chromedevtools.github.io/devtools-protocol/tot/ <br>
Opening the **remote-debugging-port** allows you to get insight about how the browser is "behaving" or "performing", for example :<br>
- console errors (javascript, css, cross-side-scripting, etc)
- page loading data (start loading, start rendering, end executing tasks, etc)
- page layout data (client height, client width, etc)
- script starting, executing, jsHeapSize, detached elements, frames, nodes, etc.

## Destination data
At the moment there are two possible data destination:<br>
- local file system: the application will try to create files in <code>c:\temp</code>
- (optional) Elasticsearch/Opensearch target. You'll need to configure url, index name, accesskey and secret to send documents to Elastic/Opensearch<br>
  opensearch_url=https://myelasticurl.com:443 <br>
  opensearch_index=chrome_rdp <br>
  opensearch_accesskey=MyAcc3ssKey <br>
  opensearch_secret=S3cr3t <br>
