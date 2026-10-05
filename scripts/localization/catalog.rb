#!/usr/bin/env ruby
# Offline Unity Localization catalog audit. Never contacts Google or deletes entries.
require 'yaml'
require 'json'
require 'digest'
require 'optparse'

module Catalog
  def self.yaml(path)
    text = File.read(path)
    YAML.safe_load(text.sub(/\A%YAML[^\n]*\n%TAG[^\n]*\n---[^\n]*\n/, ''))['MonoBehaviour']
  end

  def self.inventory(root)
    Dir.glob(File.join(root, 'Assets/Localization/Tables/**/* Shared Data.asset')).map do |path|
      shared = yaml(path)
      entries = shared.fetch('m_Entries', []) || []
      tables = {}
      Dir.glob(File.join(File.dirname(path), '*.asset')).each do |candidate|
        data = yaml(candidate)
        locale = data.dig('m_LocaleId', 'm_Code')
        next unless locale
        tables[locale] = { 'path' => candidate, 'rows' => data.fetch('m_TableData', []) || [] }
      end
      { 'collection' => shared.fetch('m_TableCollectionName'), 'path' => path,
        'entries' => entries, 'tables' => tables }
    end
  end

  # Positional composite-format arguments, including numeric format specifiers.
  # Smart-format named arguments are preserved by name; plural branches are not translated here.
  def self.arguments(text)
    text.to_s.gsub('{{', '').gsub('}}', '').scan(/\{([\w.]+)(?:[,}:])/).flatten.uniq.sort
  end

  def self.tags(text)
    stack = []; errors = []
    text.to_s.scan(/<\s*(\/?)\s*([a-zA-Z]+)([^>]*)>/).each do |closing, name, tail|
      name = name.downcase
      next if %w[br sprite space quad].include?(name) || tail.strip.end_with?('/')
      if closing.empty?
        stack << name
      elsif stack.last == name
        stack.pop
      else
        errors << "unmatched closing #{name}"
      end
    end
    errors + stack.map { |name| "unclosed #{name}" }
  end

  def self.format_errors(text)
    stripped = text.to_s.gsub('{{', '').gsub('}}', '')
    depth = 0; errors = []
    stripped.each_char do |ch|
      depth += 1 if ch == '{'
      depth -= 1 if ch == '}'
      if depth < 0
        errors << 'unmatched format closing brace'; depth = 0
      end
    end
    errors << 'unclosed format argument' unless depth.zero?
    errors
  end

  # Balanced format signatures retain selectors/specifiers and nested argument
  # structure while allowing the natural-language branches of SmartStrings to vary.
  def self.format_signatures(text)
    tokens = []; errors = []; i = 0
    parse = nil
    parse = lambda do |nested|
      plain = ''; children = []; pipes = 0
      while i < text.length
        ch = text[i]
        if ch == '{'
          if text[i + 1] == '{'
            tokens << '{{'; i += 2; next
          end
          i += 1
          header = ''
          while i < text.length && ![':', '}', '{'].include?(text[i])
            header << text[i]; i += 1
          end
          if i >= text.length
            errors << 'unclosed argument'; break
          elsif text[i] == '}'
            tokens << "{#{header}}"; children << header; i += 1
          elsif text[i] == ':'
            i += 1; body, descendants, branches = parse.call(true)
            if descendants.empty? && branches.zero?
              tokens << "{#{header}:#{body}}"
            else
              formatter = body.include?(':') ? body.split(':', 2).first + ':' : ''
              tokens << "{#{header}:#{formatter}branches=#{branches + 1};nested=#{descendants.join(',')}}"
            end
            children << header
          else
            errors << 'invalid argument header'; i += 1
          end
        elsif ch == '}'
          if nested
            i += 1; return [plain, children, pipes]
          elsif text[i + 1] == '}'
            tokens << '}}'; i += 2
          else
            errors << 'unmatched closing brace'; i += 1
          end
        else
          pipes += 1 if ch == '|'
          plain << ch; i += 1
        end
      end
      errors << 'unclosed argument body' if nested
      [plain, children, pipes]
    end
    parse.call(false)
    { 'tokens' => tokens.sort, 'errors' => errors }
  end

  def self.translation_review(source, translated, numeric_review = nil)
    problems = []; numeric_findings = []; originals = source.fetch('entries'); rows = translated.fetch('entries')
    expected_hash = source['englishHash'] || source['sourceHash']
    hashes = [translated['sourceHash'], translated['englishHash']].compact
    problems << { 'type' => 'sourceHashMismatch', 'expected' => expected_hash, 'actual' => hashes } if !expected_hash || hashes.empty? || hashes.any? { |hash| hash != expected_hash }
    problems << { 'type' => 'missingLocale' } if translated['locale'].to_s.empty?
    reviewed_rows = []
    if numeric_review
      if numeric_review['sourceHash'] != expected_hash || numeric_review['locale'] != translated['locale']
        problems << { 'type' => 'numericReviewMetadataMismatch', 'expectedSourceHash' => expected_hash, 'expectedLocale' => translated['locale'] }
      elsif !(numeric_review['rows'] || numeric_review['entries']).is_a?(Array)
        problems << { 'type' => 'invalidNumericReviewRows' }
      else
        reviewed_rows = numeric_review['rows'] || numeric_review['entries']
      end
    end
    identity = lambda { |row| [row['collection'], row['id'].to_s, row['key']] }
    rows.group_by { |row| identity.call(row) }.each do |key, group|
      problems << { 'type' => 'duplicateEntry', 'identity' => key } if group.length > 1
    end
    rows.group_by { |row| [row['collection'], row['id'].to_s] }.each do |key, group|
      problems << { 'type' => 'duplicateId', 'identity' => key } if group.length > 1
    end
    original_map = originals.map { |row| [identity.call(row), row] }.to_h
    translated_map = rows.map { |row| [identity.call(row), row] }.to_h
    (original_map.keys - translated_map.keys).each { |key| problems << { 'type' => 'missingEntry', 'identity' => key } }
    (translated_map.keys - original_map.keys).each { |key| problems << { 'type' => 'unexpectedEntry', 'identity' => key } }
    original_map.each do |key, original|
      translated_row = translated_map[key]; next unless translated_row
      value = translated_row.key?('translation') ? translated_row['translation'] : translated_row['text']
      if !value.is_a?(String)
        problems << { 'type' => 'invalidTranslationType', 'identity' => key }; next
      end
      english = original.fetch('english')
      problems << { 'type' => 'blankMismatch', 'identity' => key } if english.empty? != value.empty?
      if translated_row.key?('translation') && translated_row.key?('text') && translated_row['text'] != value
        problems << { 'type' => 'translationFieldConflict', 'identity' => key }
      end
      a = format_signatures(english); b = format_signatures(value)
      problems << { 'type' => 'formatMismatch', 'identity' => key, 'source' => a, 'translation' => b } if a['tokens'] != b['tokens'] || !b['errors'].empty?
      { 'markup' => /<[^>]+>/, 'numbers' => /\d+(?:[.,]\d+)*/, 'controls' => /[\x00-\x1f]/, 'escapedControls' => /\\[nrt]/ }.each do |name, pattern|
        aa = english.scan(pattern).sort; bb = value.scan(pattern).sort
        next if aa == bb
        review = name == 'numbers' && reviewed_rows.find do |item|
          identity.call(item) == key && item['english'] == english && item['translation'] == value &&
            item['reason'].is_a?(String) && !item['reason'].strip.empty?
        end
        if review
          numeric_findings << { 'type' => 'reviewedNumericDifference', 'identity' => key, 'source' => aa, 'translation' => bb, 'reason' => review['reason'] }
        else
          problems << { 'type' => name + 'Mismatch', 'identity' => key, 'source' => aa, 'translation' => bb }
        end
      end
    end
    { 'schemaVersion' => 1, 'readOnly' => true, 'locale' => translated['locale'], 'sourceHash' => expected_hash,
      'sourceCount' => originals.length, 'translationCount' => rows.length, 'mismatches' => problems,
      'numericReviewFindings' => numeric_findings, 'valid' => problems.empty?, 'linguisticReviewRequired' => true }
  end

  def self.authored_inventory(root)
    entries = {}; conflicts = []
    visit = lambda do |node, path|
      if node.is_a?(Hash)
        if node['key'].is_a?(String) && node['fallback'].is_a?(String) && !node['key'].empty?
          key = node['key']; value = node['fallback']
          conflicts << { 'key' => key, 'english' => value, 'source' => path } if entries[key] && entries[key]['english'] != value
          entries[key] ||= { 'key' => key, 'english' => value, 'sources' => [] }
          entries[key]['sources'] << path
        end
        node.each_value { |v| visit.call(v, path) }
      elsif node.is_a?(Array)
        node.each { |v| visit.call(v, path) }
      end
    end
    Dir.glob(File.join(root, 'Assets/UI/Toolkit/*.asset')).each { |path| visit.call(yaml(path), path) }
    { 'entries' => entries.values, 'conflicts' => conflicts }
  end

  def self.audit(collections)
    errors = []; coverage = []
    locales = collections.flat_map { |c| c['tables'].keys }.uniq
    collections.each do |c|
      ids = c['entries'].map { |e| e['m_Id'] }; keys = c['entries'].map { |e| e['m_Key'] }
      [ ['ID', ids], ['key', keys] ].each do |label, values|
        values.group_by { |v| v }.each { |v, group| errors << "#{c['collection']}: duplicate #{label} #{v}" if group.length > 1 }
      end
      (locales - c['tables'].keys).each { |locale| coverage << { 'collection' => c['collection'], 'locale' => locale, 'missingTable' => true, 'missingIds' => ids, 'blankIds' => [] } }
      en = (c['tables'].dig('en', 'rows') || []).map { |r| [r['m_Id'], r['m_Localized'].to_s] }.to_h
      c['tables'].each do |locale, table|
        rows = table['rows']; rowids = rows.map { |r| r['m_Id'] }
        rowids.group_by { |id| id }.each { |id, group| errors << "#{c['collection']}/#{locale}: duplicate ID #{id}" if group.length > 1 }
        rows.each do |row|
          id = row['m_Id']; value = row['m_Localized'].to_s
          errors << "#{c['collection']}/#{locale}: orphan ID #{id}" unless ids.include?(id)
          (tags(value) + format_errors(value)).each { |e| errors << "#{c['collection']}/#{locale}/#{id}: #{e}" }
          if locale != 'en' && !value.empty? && en[id] && arguments(value) != arguments(en[id])
            errors << "#{c['collection']}/#{locale}/#{id}: argument mismatch #{arguments(en[id])} vs #{arguments(value)}"
          end
        end
        coverage << { 'collection' => c['collection'], 'locale' => locale,
          'missingIds' => ids - rowids, 'blankIds' => rows.select { |r| r['m_Localized'].to_s.strip.empty? }.map { |r| r['m_Id'] } }
      end
    end
    { 'errors' => errors, 'coverage' => coverage }
  end

  def self.snapshot(collections)
    collections.flat_map do |c|
      c['entries'].map do |entry|
        values = c['tables'].map { |locale, table| [locale, (table['rows'].find { |r| r['m_Id'] == entry['m_Id'] } || {})['m_Localized']] }.to_h
        { 'collection' => c['collection'], 'id' => entry['m_Id'].to_s, 'key' => entry['m_Key'], 'values' => values }
      end
    end
  end

  def self.preflight(local, sheet)
    results = []
    sheet.fetch('tabs').each do |tab|
      collection = tab.fetch('title'); rows = tab.fetch('rows')
      own = local.select { |r| r['collection'] == collection }
      rows.group_by { |r| r['key'] }.each { |key, group| results << { 'type' => 'duplicateSheetKey', 'collection' => collection, 'key' => key } if group.length > 1 }
      rows.group_by { |r| r['numericIdNote'].to_s }.each { |id, group| results << { 'type' => 'duplicateOrMissingSheetId', 'collection' => collection, 'id' => id } if id.empty? || group.length > 1 }
      own.each do |entry|
        remote = rows.find { |r| r['key'] == entry['key'] }
        unless remote
          results << entry.merge('type' => 'localOnly'); next
        end
        if remote['numericIdNote'].to_s != entry['id']
          results << entry.merge('type' => 'idMismatch', 'sheetId' => remote['numericIdNote']); next
        end
        %w[en ru].each do |locale|
          sheetvalue = remote[locale == 'en' ? 'english' : 'russian'].to_s
          localvalue = entry['values'][locale].to_s
          results << { 'type' => 'valueDifference', 'collection' => collection, 'key' => entry['key'], 'id' => entry['id'], 'locale' => locale, 'local' => localvalue, 'sheet' => sheetvalue } if localvalue != sheetvalue
        end
      end
      rows.each { |row| results << row.merge('collection' => collection, 'type' => 'sheetOnly') unless own.any? { |r| r['key'] == row['key'] } }
    end
    { 'schemaVersion' => 1, 'readOnly' => true, 'snapshotObservedAt' => sheet['observedAt'], 'differences' => results }
  end

  def self.sync(collections, manifest, apply, expected)
    edits = {}; additions = []; conflicts = []
    manifest.each do |entry|
      c = collections.find { |item| item['collection'] == (entry['collection'] || 'TownUI') }
      raise "Unknown collection #{entry['collection']}" unless c && c['tables']['en']
      key = entry.fetch('key'); english = entry.fetch('english')
      existing = c['entries'].find { |item| item['m_Key'] == key }
      if existing
        id = existing['m_Id']
        conflicts << "#{key}: proposed ID differs" if entry['id'] && entry['id'].to_s != id.to_s
        row = c['tables']['en']['rows'].find { |item| item['m_Id'] == id }
        if row
          conflicts << "#{key}: English differs; review source and translations explicitly" if row['m_Localized'].to_s != english
          next
        end
      else
        id = entry['id'] ? Integer(entry['id']) : ([87000000000002999] + collections.flat_map { |item| item['entries'].map { |r| r['m_Id'].to_i } }.select { |v| v >= 87000000000003000 && v < 88000000000000000 }).max + 1
        raise "Invalid or colliding ID #{id}" unless id > 0 && !c['entries'].any? { |item| item['m_Id'] == id }
        shared = edits[c['path']] ||= File.read(c['path'])
        # Insert at the start of existing list; all old entries/metadata stay byte-identical.
        shared.sub!(/  m_Entries:[ \t]*\n/) { "  m_Entries:\n  - m_Id: #{id}\n    m_Key: #{JSON.generate(key)}\n    m_Metadata:\n      m_Items: []\n" } or raise 'Unsupported shared entries layout'
        c['entries'] << { 'm_Id' => id, 'm_Key' => key }
      end
      path = c['tables']['en']['path']; text = edits[path] ||= File.read(path)
      text.sub!(/  m_TableData:[ \t]*\n/) { "  m_TableData:\n  - m_Id: #{id}\n    m_Localized: #{JSON.generate(english)}\n    m_Metadata:\n      m_Items: []\n" } or raise 'Unsupported table layout'
      c['tables']['en']['rows'] << { 'm_Id' => id, 'm_Localized' => english }
      additions << { 'collection' => c['collection'], 'key' => key, 'id' => id.to_s }
    end
    raise conflicts.join("\n") unless conflicts.empty?
    validation = audit(collections)
    # Existing legacy format issues are reported by audit; only proposed strings gate additions.
    manifest.each { |e| issues = tags(e['english']) + format_errors(e['english']); raise "#{e['key']}: #{issues.join(', ')}" unless issues.empty? }
    hashes = edits.keys.map { |p| [p, Digest::SHA256.file(p).hexdigest] }.to_h
    if apply
      raise 'Apply requires --expected HASH_REPORT from reviewed dry-run' unless expected
      review = JSON.parse(File.read(expected))
      raise 'Manifest changed since dry-run; rerun and review' unless review['manifestHash'] == Digest::SHA256.hexdigest(JSON.generate(manifest))
      expectedhashes = review.fetch('inputHashes')
      raise 'Catalog changed since dry-run; rerun and review' unless hashes == expectedhashes
      edits.each { |path, content| File.write(path, content) }
    end
    { 'manifestHash' => Digest::SHA256.hexdigest(JSON.generate(manifest)), 'dryRun' => !apply, 'additions' => additions, 'inputHashes' => hashes, 'validation' => validation }
  end
end

if $PROGRAM_NAME == __FILE__
  options = { root: Dir.pwd, apply: false }
  parser = OptionParser.new do |o|
    o.banner = 'catalog.rb audit|snapshot|changes|sheet-preflight|sync-english|coverage|inventory|validate-translation [options]'
    o.on('--root PATH') { |v| options[:root] = v }
    o.on('--numeric-review JSON') { |v| options[:numeric_review] = v }
    o.on('--source JSON') { |v| options[:source] = v }
    o.on('--input JSON') { |v| options[:input] = v }
    o.on('--output JSON') { |v| options[:output] = v }
    o.on('--expected JSON') { |v| options[:expected] = v }
    o.on('--apply') { options[:apply] = true }
  end
  begin
    command = ARGV.shift; parser.parse!
    collections = command == 'validate-translation' ? [] : Catalog.inventory(options[:root]); local = Catalog.snapshot(collections)
    result = case command
    when 'validate-translation' then Catalog.translation_review(JSON.parse(File.read(options.fetch(:source))), JSON.parse(File.read(options.fetch(:input))), options[:numeric_review] && JSON.parse(File.read(options[:numeric_review])))
    when 'inventory' then Catalog.authored_inventory(options[:root])
    when 'audit' then Catalog.audit(collections)
    when 'coverage'
      input = JSON.parse(File.read(options.fetch(:input))); input = input['entries'] if input.is_a?(Hash)
      { 'missing' => input.reject { |entry| local.any? { |row| row['collection'] == (entry['collection'] || 'TownUI') && row['key'] == entry['key'] && row.dig('values', 'en') == entry['english'] } } }
    when 'snapshot' then local
    when 'sheet-preflight' then Catalog.preflight(local, JSON.parse(File.read(options.fetch(:input))))
    when 'changes'
      old = JSON.parse(File.read(options.fetch(:input)))
      local.map do |entry|
        prior = old.find { |r| r['collection'] == entry['collection'] && r['id'] == entry['id'] }
        next if prior && prior['key'] == entry['key'] && prior.dig('values', 'en') == entry.dig('values', 'en')
        entry.merge('previous' => prior, 'translationReviewRequired' => entry['values'].any? { |locale, value| locale != 'en' && value && !value.empty? })
      end.compact + old.reject { |prior| local.any? { |entry| entry['collection'] == prior['collection'] && entry['id'] == prior['id'] } }.map { |prior| prior.merge('type' => 'removedFromLocal', 'manualReviewRequired' => true) }
    when 'sync-english'
      input = JSON.parse(File.read(options.fetch(:input))); input = input['entries'] if input.is_a?(Hash)
      Catalog.sync(collections, input, options[:apply], options[:expected])
    else raise parser.to_s
    end
    json = JSON.pretty_generate(result)
    options[:output] ? File.write(options[:output], json + "\n") : puts(json)
    exit(1) if command == 'validate-translation' && !result['valid']
    exit(1) if command == 'audit' && !result['errors'].empty?
  rescue StandardError => e
    warn e.message; exit 2
  end
end
