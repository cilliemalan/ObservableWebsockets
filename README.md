# ObservableWebsockets
An IObservable interface to websockets for ASP.Net Core and OWIN.
Though it doesn't have a dependency on [Reactive Extensions](http://reactivex.io/) (because versioning is a right *pain*),
the idea is that you'll be using it.

## Magic!
```
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

## Notes
Client disconnects will not cause errors on the server side. OnComplete will be called when the client
disconnects from the client side for whatever reason (disconnect, connection drop, close message, whatever).
When an observer throws an exception, OnError will be called and OnComplete will not be called. In this case
the server connection will close with a close message without exposing an error to the client.

# License
Copyright (c) 2019 Cillié Malan see [LICENSE](LICENSE)