# README for Application and Protocol Configuration JSON File

This README provides an explanation for each field in the JSON configuration file for a NEO node.

## ApplicationConfiguration

### Logger
- **Path**: Directory where log files are stored. Default is "Logs".
- **ConsoleOutput**: Boolean flag to enable or disable console output for logging. Default is `false`.
- **Active**: Boolean flag to activate or deactivate the logger. Default is `false`.

### Storage
- **Engine**: Specifies the storage engine used by the node. Possible values are:
    - `MemoryStore`
    - `LevelDBStore`
    - `RocksDBStore`
- **Path**: Path to the data storage directory. `{0}` is a placeholder for the network ID.

### P2P
- **Port**: Port number for the P2P network. MainNet is `10333`, TestNet is `20333`.
- **MinDesiredConnections**: Minimum number of desired P2P connections. Default is `10`.
- **MaxConnections**: Maximum number of P2P connections. Default is `40`.
- **MaxConnectionsPerAddress**: Maximum number of connections allowed per address. Default is `3`.

### UnlockWallet
- **Path**: Path to the wallet file.
- **Password**: Password for the wallet.
- **IsActive**: Boolean flag to activate or deactivate the wallet. Default is `false`.

### Contracts
- **NeoNameService**: Script hash of the Neo Name Service contract. MainNet is `0x50ac1c37690cc2cfc594472833cf57505d5f46de`, TestNet is `0x50ac1c37690cc2cfc594472833cf57505d5f46de`.

### Plugins
- **DownloadUrl**: URL to download plugins, typically from the NEO project's GitHub releases. Default is `https://api.github.com/repos/neo-project/neo/releases`.

## ProtocolConfiguration

### Network
- **Network**: Network ID for the NEO network. MainNet is `860833102`, TestNet is `894710606`

### AddressVersion
- **AddressVersion**: Version byte used in NEO address generation. Default is `53`.

### MillisecondsPerBlock
- **MillisecondsPerBlock**: Time interval between blocks in milliseconds. Default is `15000` (15 seconds).

### MaxTransactionsPerBlock
- **MaxTransactionsPerBlock**: Maximum number of transactions allowed per block. Default is `512`.

### MemoryPoolMaxTransactions
- **MemoryPoolMaxTransactions**: Maximum number of transactions that can be held in the memory pool. Default is `50000`.

### MaxTraceableBlocks
- **MaxTraceableBlocks**: Maximum number of blocks that can be traced back. Default is `2102400`.

### Hardforks
- **HF_Aspidochelone**: Block height for the Aspidochelone hard fork. MainNet is `1730000`, TestNet is `210000`.
- **HF_Basilisk**: Block height for the Basilisk hard fork. MainNet is `4120000`, TestNet is `2680000`.
- **HF_Cockatrice**: Block height for the Cockatrice hard fork. MainNet is `5450000`, TestNet is `3967000`.

### InitialGasDistribution
- **InitialGasDistribution**: Total amount of GAS distributed initially. Default is `5,200,000,000,000,000 Datoshi` (`52,000,000 GAS`).

### ValidatorsCount
- **ValidatorsCount**: Number of consensus validators. Default is `7`.

### StandbyCommittee
- **StandbyCommittee**: List of public keys for the standby committee members.

### SeedList
- **SeedList**: List of seed nodes with their addresses and ports.
  - MainNet addresses are:
      - `seed1.epicchain.org:90555`
      - `seed2.epicchain.org:90555`
      - `seed3.epicchain.org:90555`
      - `seed4.epicchain.org:90555`
      - `seed5.epicchain.org:90555`
  - TestNet addresses are:
      - `seed1t5.epicchain.org:80555`
      - `seed2t5.epicchain.org:80555`
      - `seed3t5.epicchain.org:80555`
      - `seed4t5.epicchain.org:80555`
      - `seed5t5.epicchain.org:80555`

This configuration file is essential for setting up and running a NEO node, ensuring proper logging, storage, network connectivity, and consensus protocol parameters.
