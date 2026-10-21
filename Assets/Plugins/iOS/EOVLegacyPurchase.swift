import Foundation
import StoreKit

@_silgen_name("UnitySendMessage")
private func sendToUnity(_ object: UnsafePointer<CChar>, _ method: UnsafePointer<CChar>, _ message: UnsafePointer<CChar>)

@_cdecl("EOV_CheckLegacyPurchase")
public func checkLegacyPurchase(_ object: UnsafePointer<CChar>, _ cutoff: Double) {
    let name = String(cString: object)
    if #available(iOS 16.0, *) {
        Task { @MainActor in
            var verified = false
            var eligible = false
            do {
                let result = try await AppTransaction.shared
                if case .verified(let transaction) = result, transaction.environment == .production {
                    verified = true
                    eligible = transaction.originalPurchaseDate.timeIntervalSince1970 < cutoff
                }
            } catch { /* Retry on the next restore or launch. */ }
            let json = "{\"verified\":\(verified),\"eligible\":\(eligible)}"
            name.withCString { objectPointer in
                "OnLegacyAppleResult".withCString { methodPointer in
                    json.withCString { messagePointer in sendToUnity(objectPointer, methodPointer, messagePointer) }
                }
            }
        }
    }
}
