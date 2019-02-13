# ObservableWebsockets
An IObservable interface to websockets for ASP.Net Core and OWIN.

Though it doesn't have a dependency on [Reactive Extensions](http://reactivex.io/) (because versioning is a right *pain*),
the idea is that you'll be using it.

## Magic!
```csharp
app.UseObservableWebsockets(c =>
    c.OnAccept(ws =>
    {
        // ws exposes the websocket as an IObservable.
        // it also has some methods

        // sent messages are queued. No more async errors!
        ws.SendText("You have connected!");

        // using Reactive Extensions!
        ws.Where(x => x.messageType == WebSocketMessageType.Text)
            .Select(x => x.message.Decode())
            .Where(x => x.Contains("hello"))
            .Subscribe(x =>
            {
                ws.SendText($"hello to you too! Your message was {x.Length} long.");
            });
    }));
```

## Portability
The library works with old ASP.Net using OWIN, and new ASP.Net Core 2+ (1.3 might work but not tested).
Small inconsistencies between ASP.Net core and OWIN have also been resolved.

As mentioned above, I avoided the System.Reactive dependency because if you're using an old version your life
will become absolute hell. It's not necessary to use Reactive Extensions. The simplification
of the WebSocket interface might even make things simpler. However, the real magic happens when you use it.
For example, I've used it to stream logs from SeriLog right out of a websocket, and connect a websocket to an
IObservable MQTT interface. Both of these were only about a dozen lines of neat composable code because Rx
makes observable sequences so easy.

Note: examples aren't verbatim because the interface is slightly different and some helper methods aren't in this library yet. I'll update it at a later date.

### MQTT example
```csharp
// an observable mqtt interface
var mqtt = app.ApplicationServices.GetService<MqttConnection>();
var publishTopic = Configuration["PublishTopic"];
var subscription = mqtt.GetSubscription(Configuration["SubscribeTopic"]);

// incoming MQTT messages are piped out
subscription.Messages
    .Select(m => Encoding.UTF8.GetString(m.Payload))
    .Where(m => Regex.IsMatch(m, "off|on"))
    .Subscribe(m => ws.SendText(m));

// messages received from the websocket
ws.AsText().Where(m => Regex.IsMatch(m, "off|on"))
    .Select(m => new MqttApplicationMessage { Payload = Encoding.UTF8.GetBytes(m), Topic = subscription.Path })
    .Subscribe(m => mqtt.Send(m));
```

### Serilog example
```csharp
// pump messages out
LogEventLevel loglevel = LogEventLevel.Verbose;
ObservableLogger.Instance
    .Where(l => l.Level >= loglevel)
    .Select(l => new { Level = l.Level.ToString(), l.Timestamp, Message = MessageString(l) })
    .Select(l => JsonConvert.SerializeObject(l))
    .Subscribe(m => ws.SendText(m));

// client controls loglevel
var sa = ws.AsText()
    .Select(m => { try { return JToken.Parse(m) as JObject; } catch { return null; } })
    .Where(m => m != null)
    .Subscribe(m =>
    {
        var lls = m.TryGetValue("logLevel", out var lljt) ? (lljt as JValue)?.Value?.ToString() : null;
        if(Enum.TryParse<LogEventLevel>(lls, out var ll))
        {
            loglevel = ll;
        }
    });
```

## Notes
Client disconnects will not cause errors on the server side. OnComplete will be called when the client
disconnects from the client side for whatever reason (disconnect, connection drop, close message, whatever).
When an observer throws an exception, OnError will be called and OnComplete will not be called. In this case
the server connection will close with a close message without exposing an error to the client.

# License
Copyright (c) 2019 Cillié Malan see [LICENSE](LICENSE)
