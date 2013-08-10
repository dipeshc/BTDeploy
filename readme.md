# BTDeploy
BTDeploy is a BitTorrent based deployment app for massive file collections that need to be distributed to many machines. Ideal for deploying file collections to servers within a data center.

### Usage
##### Add a deployment torrent.
```shell
BTDeploy add -t <path to torrent> -o <deployment directory path>
```
Process will exit immediately. Daemon process will download and seed asynchronously.


##### Add a deployment torrent and waiting until it finishes downloading.
This process will exit once downloading has completed and the seeding will continue asynchronously.
```shell
BTDeploy add -w -t <path to torrent> -o <deployment directory path>
```
Process will exit once download complete. Daemon process will seed asynchronously.


##### Add many deployment and terminate once everything has completed.
```shell
BTDeploy add -w -t <path to torrent 1> -o <deployment directory path 1>
...
BTDeploy add -w -t <path to torrent N> -o <deployment directory path N>
BTDeploy wait --stop *
```

#### Create a deployment torrent.
```shell
BTDeploy create -n <torrent name> -f <deployment file source> --trackers <comma seperated list of trackers>
```

#### Create a deployment torrent and start seeding it.
```shell
BTDeploy create -n <torrent name> -f <deployment file source> --trackers <comma seperated list of trackers> -a
```

### Handy Trick
Diffs will only be downloaded for newer versions of a file collection. This is due to the hash checking done before downloading. Hence only the blocks of files that are different will be downloaded. Used in combination with the --mirror flag, identical new versions of file collections will be deployed in no time.
