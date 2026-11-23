import XCTest
@testable import SynqKit

final class SynqKitTests: XCTestCase {
    func testApiVersion() {
        XCTAssertEqual(SynqKit.apiVersion, "v1")
    }
}
