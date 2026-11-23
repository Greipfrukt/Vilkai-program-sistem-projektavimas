// swift-tools-version:5.9
// I-02 – SynqKit: modeliai, API klientas, WS. Peržiūrimas ir testuojamas be Mac (swift test).
import PackageDescription

let package = Package(
    name: "SynqKit",
    platforms: [.iOS(.v17), .macOS(.v14)],
    products: [
        .library(name: "SynqKit", targets: ["SynqKit"]),
    ],
    targets: [
        .target(name: "SynqKit"),
        .testTarget(name: "SynqKitTests", dependencies: ["SynqKit"]),
    ]
)
