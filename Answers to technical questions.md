1. **How long did you spend on the coding assignment?**

3-4 hours

2. **What would you add to your solution if you had
more time?**


- ApiKey (not just swagger stab);
- more logging;
- error handling in middleware;
- ask questions about identity of returned data: crypto currency symbol is not globally unique and so it is not clear if customer also wants to see, for instance, tokens;

2. **What was the most useful feature that was added to the latest version of your language of choice?**

*Primary Constructors on classes*

Finally there is a way to get rid of boilerplate code in constructors. Since null checks are now mostly compiler's problem there is no need to assign arguments to private fields explicitly.

```
public class SomeService(IDependency dependency)
{
    public Task Do() => dependency.Do();
}
```


An unexpected effect: to use this way people have to drop old-school naming convention where private fields are prefixed with underscores, which I think is a right thing to do: m_ is long gone, _ also should disappear

*Utf8JsonReader*

System.Text.Json namespace is as awesome as it is weird. In theory it is great that you can convert binary data to objects without intermediate "string" step, but, at the same time, you need those strings for logging; so, to fully utilize power of System.Text.Json logging api should support utf8-strings, otherwise instead of saving resources you waste resources and increase complexity.
 
3. **How would you track down a performance issue in production? Have you ever had to do this?**

requirements:
- correlation-id \ trace-id
- logging system of your choice (kibana)
- access to cloud metrics (assuming we are using cloud)

process:
- ask how does the reporting person know that there is a performance problem - evidence (unless it is automatic perf. alert, which makes it easier)
- quick check for global error count in logging system (or metrics), if anything stands out
- see if there are noticable physical problems on cloud level (cpu, memory, free space issues, resource usage limits, throttling, request count, drop in response time); if there are, are they new or old? were there any restarts, scale ups or scale outs recently
- were there deployments recently
- who is affected - one client or all clients
- hopefuly it is clear from metric\logging which handler is affected
- if any of cloud perm metrics is elevated - scope is narrow enough to look for particular problems
- if metrics are fine - delay could be in cloud infrastructure (messaging brokers, for instance)
- ...

4. **What was the latest technical book you have read or tech conference you have been to? What did you learn?**
- linux kernel programming
- mostly was looking for differences between windows and linux internals: the most important I think is that linux does not have strict hierarchy of interrupt levels
- must say that quality of literature (writing mostly) in Windows world is much higher

5. **What do you think about this technical assessment?**

It looked easy until I understood that cryto-currency code (BTC) is not globally unique - there also could be naming collisions and child entities (like tokens). Also coinmarketcap does not support multiple fiat currencies for FREE plans which you are not aware of when you work with test api.

There are also some conflicting requirements: to make the solution run in one step then api-key for coinmarketcap must be somehow known, which means it has to be committed to git, which is number-1 bad practice nowadays.

6. **Please, describe yourself using JSON.**

```
{
	"name": "Igor",
	"city": "Amsterdam",
	"occupations": {
		"software-development": {
			"preferred-language": "C#",
			"style": {
				"imperative": 0.4,
				"functional": 0.4,
				"declarative": 0.2,
				"onion": 0.3,
				"gof": 0.2,
				"p&p": 0.2,
				"bdd": 0.9,
				"tdd": 0.0,
				"unit-tests": 0.1,
				"integration-tests": 0.9,
				"events": 0.8,
				"http-chains": 0.2,
				"frontend": 0.1,
				"backend": 0.9,
				"data": 1.0
			}
		}
	},
	"reading": {
		"professional": [
			"internals",
			"architecture"
		],
		"fiction": [
			"hard-sci-fi",
			"sci-fi",
			"literature-of-absurd"
		],
		"non-fiction": [
			"pop-sience"
		]
	},
	"sports": {
		"road-cycling": {
			"bike": "Cannondale SystemSix",
			"since-year": "2019",
			"total-distance-km": 15147,
			"total-hours": 568,
			"max-distance-per-day-km": 350,
			"best-twenty-minutes-power-watt": 320,
			"koms": 3,
			"goals": [
				"400 watt 20 minutes"
			]
		}
	},
	"music": [
		"Thrash Metal"
	]
}
```
