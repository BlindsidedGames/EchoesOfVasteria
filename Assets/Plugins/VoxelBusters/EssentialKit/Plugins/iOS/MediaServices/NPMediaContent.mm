//
//  NPMediaContent.mm
//  Native Plugins
//
//  Created by Ayyappa on 26/06/24.
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.
//
#import "NPMediaContent.h"
#import <UniformTypeIdentifiers/UTCoreTypes.h>
#import "NPUnityDataTypes.h"
#import "NPBindingHelper.h"

@implementation NPMediaContent //Should be implemented by other dervived classes

-(void) asRawData: (AsRawDataCompletionCallback) callback
{
    [[NSException exceptionWithName:NSInvalidArgumentException
                                      reason:[NSString stringWithFormat:@"You must override %@ in a subclass to use this method", NSStringFromSelector(_cmd)]
                                    userInfo:nil] raise];
}

-(void) asFilePath: (NSString*) destinationDirectory withFileName:(NSString*) fileName withCallback:(AsFilePathCompletionCallback) callback
{
    [[NSException exceptionWithName:NSInvalidArgumentException
                                      reason:[NSString stringWithFormat:@"You must override %@ in a subclass to use this method", NSStringFromSelector(_cmd)]
                                    userInfo:nil] raise];
}

@end


@interface NPItemProviderMediaContent ()
@property (nonatomic, strong) NSItemProvider* itemProvider;
@end

@implementation NPItemProviderMediaContent
@synthesize itemProvider;

-(instancetype) initWithItemProvider:(NSItemProvider*) itemProvider
{
    self = [super init];
    self.itemProvider = itemProvider;
    
    return self;
}

-(void) asRawData: (AsRawDataCompletionCallback) callback
{
    //NSLog(@"registeredTypeIdentifiers : %@", self.itemProvider.registeredTypeIdentifiers);
    UTType* type = [self getType: self.itemProvider];
    [self.itemProvider loadDataRepresentationForTypeIdentifier:type.identifier
                                             completionHandler:^(NSData * _Nullable data, NSError * _Nullable error) {
        if([self.itemProvider hasItemConformingToTypeIdentifier:UTTypeImage.identifier]) {
            UIImage *image = [UIImage imageWithData:data];
            callback(NPEncodeImageAsData(image, UIImageEncodeTypePNG), UTTypePNG.preferredMIMEType, nil);
        } else {
            callback(data, type.preferredMIMEType, [NPMediaServicesError createFrom:error]);
        }
    }];
}

-(void) asFilePath: (NSString*) destinationDirectory withFileName:(NSString*) fileName withCallback:(AsFilePathCompletionCallback) callback
{
    UTType* type = [self getType: self.itemProvider];

    [self.itemProvider loadFileRepresentationForTypeIdentifier:type.identifier
                                             completionHandler:^(NSURL * _Nullable url, NSError * _Nullable error) {
        
        if(error != nil)
        {
            callback(nil, [NPMediaServicesError createFrom:error]);
            return;
        }
        
        NSFileManager *fileManager = [NSFileManager defaultManager];
        NSString *filePath = [NSString stringWithFormat:@"%@/%@.%@",destinationDirectory, fileName, type.preferredFilenameExtension];
        NSURL *destinationUrl = [[NSURL alloc] initFileURLWithPath:filePath];
        
        //Create required directories
        [fileManager createDirectoryAtPath:filePath withIntermediateDirectories:TRUE attributes:nil error:nil];
        
        NSError *fileMoveError;
        [fileManager removeItemAtURL:destinationUrl error:nil];
        [fileManager moveItemAtURL:url toURL:destinationUrl error:&fileMoveError];
                
        callback(filePath, [NPMediaServicesError createFrom:fileMoveError]);
    }];
}

-(UTType*) getType:(NSItemProvider*) itemProvider
{
    UTType* type = UTTypeContent;
    NSString *typeIdentifier = [self.itemProvider.registeredTypeIdentifiers objectAtIndex:0];
    if(typeIdentifier != nil)
    {
        // Any overrides goes here
        if([typeIdentifier containsString:@"live-photo"])
        {
            NSLog(@"Considering jpeg for live photo formats.");
            type = UTTypeJPEG;
        }
        else
        {
            type = [UTType typeWithIdentifier:typeIdentifier];
        }
    }
    
    return type;
}


@end


@interface NPRawDataMediaContent ()
@property (nonatomic, strong) NSData* contentData;
@property (nonatomic, strong) NSString* mime;
@end

@implementation NPRawDataMediaContent
@synthesize contentData;
@synthesize mime;

-(instancetype) initWithContentData:(NSData*) contentData withMime:(NSString*) mime
{
    self = [super init];
    self.contentData    = contentData;
    self.mime           = mime;
    return self;
}

-(void) asRawData: (AsRawDataCompletionCallback) callback
{
    callback(contentData, mime, [self getErrorIfNoData]);
}

-(void) asFilePath: (NSString*) destinationDirectory withFileName:(NSString*) fileName withCallback:(AsFilePathCompletionCallback) callback
{
    NPMediaServicesError* error = [self getErrorIfNoData];
    
    if(error != nil)
    {
        callback(nil, error);
        return;
    }
    
    UTType *type = [UTType typeWithMIMEType:mime];
    NSString *filePath = [NSString stringWithFormat:@"%@/%@.%@",destinationDirectory, fileName, type.preferredFilenameExtension];
    NSURL *destinationUrl = [[NSURL alloc] initFileURLWithPath:filePath];
    
    //Create required directories
    NSFileManager *fileManager = [NSFileManager defaultManager];
    [fileManager createDirectoryAtPath:filePath withIntermediateDirectories:TRUE attributes:nil error:nil];
    [fileManager removeItemAtURL:destinationUrl error:nil];
    
    
    [contentData writeToURL:destinationUrl atomically:YES];
            
    callback(filePath, nil);
}

-(NPMediaServicesError*) getErrorIfNoData
{
    if(contentData == nil)
    {
        return [NPMediaServicesError dataNotAvailable];
    }
    
    return nil;
}

@end

@interface NPContentUrlMediaContent ()
@property (nonatomic, strong) NSURL* contentUrl;
@end

@implementation NPContentUrlMediaContent
@synthesize contentUrl;

-(instancetype) initWithContentUrl:(NSURL *)contentUrl
{
    self = [super init];
    self.contentUrl    = contentUrl;
    return self;
}

-(void) asRawData: (AsRawDataCompletionCallback) callback
{
    NSData *data = [NSData dataWithContentsOfURL:contentUrl];
    callback(data, [contentUrl getMimeTypeFromUrl], [self getErrorIfNoData]);
}

-(void) asFilePath: (NSString*) destinationDirectory withFileName:(NSString*) fileName withCallback:(AsFilePathCompletionCallback) callback
{
    NPMediaServicesError* error = [self getErrorIfNoData];
    
    if(error != nil)
    {
        callback(nil, error);
        return;
    }
    
    UTType *type = [UTType typeWithMIMEType:[contentUrl getMimeTypeFromUrl]];
    NSString *filePath = [NSString stringWithFormat:@"%@/%@.%@",destinationDirectory, fileName, type.preferredFilenameExtension];
    NSURL *destinationUrl = [[NSURL alloc] initFileURLWithPath:filePath];
    
    //Create required directories
    NSFileManager *fileManager = [NSFileManager defaultManager];
    [fileManager createDirectoryAtPath:filePath withIntermediateDirectories:TRUE attributes:nil error:nil];
    [fileManager removeItemAtURL:destinationUrl error:nil];
    
    NSError* moveError;
    [fileManager moveItemAtURL:contentUrl toURL:destinationUrl error:&moveError];
    if(moveError != nil) {
        NSLog(@"Error when moving: %@", moveError);
    }
    
    callback(moveError != nil ? filePath : nil, [NPMediaServicesError createFrom:moveError]);
}

-(NPMediaServicesError*) getErrorIfNoData
{
    if(contentUrl == nil)
    {
        return [NPMediaServicesError dataNotAvailable];
    }
    
    return nil;
}
@end

@implementation NSURL (Mime)
-(NSString*) getMimeTypeFromUrl
{
    NSString* extension = [self pathExtension];
    UTType *type = [UTType typeWithFilenameExtension:extension];
    return type.preferredMIMEType;
}
@end
