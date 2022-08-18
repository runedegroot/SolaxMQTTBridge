# SolaxMQTTBridge

## Description

This project was created to forward MQTT messages sent by a Solax inverter to a private MQTT server.

Messages that are sent by the inverter are interpreted and transformed into a more readable format. Next to that it keeps bloat of your private MQTT.
Next to that this project also solves an issue when receiving the messages via Mosquitto, since the messages sent out by the inverter are in an invalid format.

> The `PUBLISH` packets sent by the inverter are invalid as the packet identifier is set to zero. This can be worked around by patching Mosquitto and removing this check from handle_publish.c. If you don’t do this then Mosquitto will detect a protocol violation and close the connection.

As mentioned in [this](https://juju.nz/michaelh/post/2021/solax/) blog post by _Michael Hope_.

In order to get the application working, forward the `mqtt001.solaxcloud.com` and `mqtt002.solaxcloud.com` to the application. The Solax inverter expects the MQTT server to be on port `2901`.

See the `docker-compose.yml` for a working configuration with environment variables.

Sensor values should become available if MQTT autodiscovery is enabled.

## License

This project is licensed under the MIT License License - see the LICENSE.md file for details

## Dependencies

* Docker
* Docker Compose
* MQTTnet
* Mosquitto

## Acknowledgments

Inspiration, code snippets, etc.
* [blog post by Blue Duck Valley Rd](https://juju.nz/michaelh/post/2021/solax/)
* [interpreted inverter values by squishykid](https://github.com/squishykid/solax/tree/master/solax/inverters)