//
//  NPMediaContent.h
//  Native Plugins
//
//  Created by Ayyappa on 26/06/24.
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.
//

#import "NPMediaServicesError.h"

typedef void (^AsRawDataCompletionCallback)(NSData* data, NSString* mime, NPMediaServicesError* error);
typedef void (^AsFilePathCompletionCallback)(NSString* path, NPMediaServicesError* error);


@interface NPMediaContent: NSObject

-(void) asRawData: (AsRawDataCompletionCallback) callback;
-(void) asFilePath: (NSString*) destinationDirectory withFileName:(NSString*) fileName withCallback:(AsFilePathCompletionCallback) callback;

@end


@interface NPItemProviderMediaContent : NPMediaContent
-(instancetype) initWithItemProvider:(NSItemProvider*) itemProvider;
@end

@interface NPRawDataMediaContent : NPMediaContent
-(instancetype) initWithContentData:(NSData*) contentData withMime:(NSString*) mime;
@end

@interface NPContentUrlMediaContent : NPMediaContent
-(instancetype) initWithContentUrl:(NSURL*) contentUrl;
@end


@interface NSURL (Mime)
-(NSString*) getMimeTypeFromUrl;
@end
