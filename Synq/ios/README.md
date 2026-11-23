# Synq iOS (Augustinas, I-01 – I-09)

- `Synq.xcodeproj` ir `Synq/` (SynqApp.swift, Info.plist su `NSAllowsLocalNetworking = YES`, Config.swift)
  kuriami Xcode'e ant Mac (I-01) – šiame skelete jų dar nėra.
- `SynqKit/` – Swift Package, kurį galima kurti ir testuoti be Xcode projekto: `cd SynqKit && swift test`.
- Modeliai (`Models/`) – Codable pagal `../Synq.yaml`.
- `Realtime/SeqTracker.swift` – tie patys principai kaip `web/src/realtime/seqTracker.ts`.
