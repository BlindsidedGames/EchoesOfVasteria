//
//  NPGameKitLeaderboardScoreEntryBinding.mm
//  Essential Kit
//
//  Created by Ayyappa Reddy on 08/06/24.
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.
//

#import <GameKit/GameKit.h>
#import "NPGameServicesDataTypes.h"
#import "NPKit.h"

#pragma mark - Native binding calls

NPBINDING DONTSTRIP long NPLeaderboardScoreEntryGetRank(void* entryPtr)
{
    GKLeaderboardEntry*    entry       = (__bridge GKLeaderboardEntry*)entryPtr;
    return entry.rank;
}

NPBINDING DONTSTRIP long NPLeaderboardScoreEntryGetValue(void* entryPtr)
{
    GKLeaderboardEntry*    entry       = (__bridge GKLeaderboardEntry*)entryPtr;
    return entry.score;
}

NPBINDING DONTSTRIP const char* NPLeaderboardScoreEntryGetLastReportedDate(void* entryPtr)
{
    GKLeaderboardEntry*    entry       = (__bridge GKLeaderboardEntry*)entryPtr;
    NSString*   date        = NPCreateNSStringFromNSDate(entry.date);
    return NPCreateCStringCopyFromNSString(date);
}

NPBINDING DONTSTRIP void* NPLeaderboardScoreEntryGetPlayer(void* entryPtr)
{
    GKLeaderboardEntry*    entry       = (__bridge GKLeaderboardEntry*)entryPtr;
    return NPRetainWithOwnershipTransfer(entry.player);
}


NPBINDING DONTSTRIP const char* NPLeaderboardScoreEntryGetTag(void* entryPtr)
{
    GKLeaderboardEntry*     entry       = (__bridge GKLeaderboardEntry*)entryPtr;
    NSUInteger              context     = entry.context;
    NSString*               tag         = nil;
    if(context != -1)
    {
        NSData *bytes = [NSData dataWithBytes:&context length:sizeof(context)];
        tag = [[NSString alloc] initWithData:bytes encoding:NSASCIIStringEncoding];
    }
    return NPCreateCStringCopyFromNSString(tag);
}
