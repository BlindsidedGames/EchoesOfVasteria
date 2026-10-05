require 'minitest/autorun'
require 'tmpdir'
require 'fileutils'
require 'open3'
require_relative 'catalog'

class CatalogBoundaryTest < Minitest::Test
  def setup
    @root = Dir.mktmpdir
    @dir = File.join(@root, 'Assets/Localization/Tables/Town'); FileUtils.mkdir_p(@dir)
    @shared = File.join(@dir, 'TownUI Shared Data.asset')
    @en = File.join(@dir, 'TownUI_en.asset'); @ru = File.join(@dir, 'TownUI_ru.asset')
    write(@shared, "  m_TableCollectionName: TownUI\n  m_Entries:\n  - m_Id: 12\n    m_Key: existing\n    m_Metadata:\n      m_Items: [translator-note]\n")
    write(@en, "  m_LocaleId: {m_Code: en}\n  m_TableData:\n  - m_Id: 12\n    m_Localized: 'Count {0}'\n    m_Metadata:\n      m_Items: [english-note]\n")
    write(@ru, "  m_LocaleId: {m_Code: ru}\n  m_TableData:\n  - m_Id: 12\n    m_Localized: 'Количество {0}'\n    m_Metadata:\n      m_Items: [translation-note]\n")
  end
  def teardown
    FileUtils.remove_entry(@root)
  end
  def write(path, body)
    File.write(path, "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n" + body)
  end
  def cli(*args)
    Open3.capture3('ruby', File.expand_path('catalog.rb', __dir__), *args, '--root', @root)
  end
  def manifest(rows)
    p = File.join(@root, 'input.json'); File.write(p, JSON.generate(rows)); p
  end
  def test_reviewed_addition_preserves_existing_bytes_ids_and_translation
    before = [@shared, @en, @ru].map { |p| File.read(p) }
    input = manifest([{ 'key' => 'new', 'english' => "A \"quote\"\nNext {0}" }])
    output, error, status = cli('sync-english', '--input', input)
    assert status.success?, error
    assert_equal before, [@shared, @en, @ru].map { |p| File.read(p) }
    review = File.join(@root, 'review.json'); File.write(review, output)
    _, error, status = cli('sync-english', '--input', input, '--apply', '--expected', review)
    assert status.success?, error
    assert_equal before[2], File.read(@ru)
    assert_includes File.read(@shared), before[0].split("  m_Entries:\n").last
    assert_includes File.read(@en), before[1].split("  m_TableData:\n").last
    rows = Catalog.yaml(@en)['m_TableData']
    assert_equal "A \"quote\"\nNext {0}", rows.find { |r| r['m_Id'] != 12 }['m_Localized']
    assert_equal 87000000000003000, rows.find { |r| r['m_Id'] != 12 }['m_Id']
  end
  def test_conflicting_english_and_changed_review_refuse_mutation
    before = File.read(@en)
    input = manifest([{ 'key' => 'existing', 'english' => 'Replacement' }])
    _, error, status = cli('sync-english', '--input', input, '--apply')
    refute status.success?; assert_includes error, 'English differs'; assert_equal before, File.read(@en)
    input = manifest([{ 'key' => 'new', 'english' => 'New' }])
    output, _, _ = cli('sync-english', '--input', input)
    review = File.join(@root, 'review.json'); File.write(review, output)
    File.write(input, JSON.generate([{ 'key' => 'new', 'english' => 'Changed manifest' }]))
    _, error, status = cli('sync-english', '--input', input, '--apply', '--expected', review)
    refute status.success?; assert_includes error, 'Manifest changed'; assert_equal before, File.read(@en)
    File.write(input, JSON.generate([{ 'key' => 'new', 'english' => 'New' }]))
    File.open(@en, 'a') { |f| f.write("  changed: true\n") }
    modified = File.read(@en)
    _, error, status = cli('sync-english', '--input', input, '--apply', '--expected', review)
    refute status.success?; assert_includes error, 'changed since'; assert_equal modified, File.read(@en)
  end
  def test_translation_argument_and_richtext_errors_are_detected
    File.write(@ru, File.read(@ru).sub('Количество {0}', '<b>Количество {1}</i> {'))
    output, _, status = cli('audit')
    refute status.success?
    errors = JSON.parse(output)['errors'].join('\n')
    assert_includes errors, 'argument mismatch'; assert_includes errors, 'unmatched closing i'; assert_includes errors, 'unclosed format argument'
  end
  def test_frozen_translation_validation_preserves_nested_format_and_rejects_incomplete_payloads
    source = { 'englishHash' => 'frozen', 'entries' => [
      { 'collection' => 'TownUI', 'id' => '12', 'key' => 'count', 'english' => "<b>{0:plural:{1:N0} apple|{1:N0} apples}</b>\n2" },
      { 'collection' => 'TownUI', 'id' => '13', 'key' => 'blank', 'english' => '' }
    ] }
    payload = { 'locale' => 'fr', 'sourceHash' => 'frozen', 'entries' => [
      { 'collection' => 'TownUI', 'id' => '12', 'key' => 'count', 'translation' => "<b>{0:plural:{1:N0} pomme|{1:N0} pommes}</b>\n2" },
      { 'collection' => 'TownUI', 'id' => '13', 'key' => 'blank', 'translation' => '' }
    ] }
    sourcepath = File.join(@root, 'frozen.json'); File.write(sourcepath, JSON.generate(source))
    input = manifest(payload)
    output, error, status = cli('validate-translation', '--source', sourcepath, '--input', input)
    assert status.success?, error
    assert JSON.parse(output)['valid']
    payload['sourceHash'] = 'stale'
    payload['entries'].pop
    payload['entries'][0]['translation'] = '<i>{0:plural:{1:N1} pomme|{1:N1} pommes}</i>3'
    File.write(input, JSON.generate(payload))
    output, _, status = cli('validate-translation', '--source', sourcepath, '--input', input)
    refute status.success?
    types = JSON.parse(output)['mismatches'].map { |row| row['type'] }
    %w[sourceHashMismatch missingEntry formatMismatch markupMismatch numbersMismatch controlsMismatch].each { |type| assert_includes types, type }
  end

  def test_numeric_review_requires_exact_reviewed_pairs_and_never_waives_structure
    source = { 'englishHash' => 'frozen', 'entries' => [
      { 'collection' => 'TownUI', 'id' => '12', 'key' => 'count', 'english' => '<b>Double {0}</b>' }
    ] }
    translated = { 'locale' => 'ja', 'sourceHash' => 'frozen', 'entries' => [
      { 'collection' => 'TownUI', 'id' => '12', 'key' => 'count', 'translation' => '<b>{0}を2倍</b>' }
    ] }
    sourcepath = File.join(@root, 'frozen.json'); File.write(sourcepath, JSON.generate(source))
    input = manifest(translated)
    review = { 'locale' => 'ja', 'sourceHash' => 'frozen', 'rows' => [
      { 'collection' => 'TownUI', 'id' => '12', 'key' => 'count', 'english' => '<b>Double {0}</b>',
        'translation' => '<b>{0}を2倍</b>', 'reason' => 'Double is expressed as 2倍 in Japanese.' }
    ] }
    reviewpath = File.join(@root, 'numeric-review.json'); File.write(reviewpath, JSON.generate(review))
    output, error, status = cli('validate-translation', '--source', sourcepath, '--input', input, '--numeric-review', reviewpath)
    assert status.success?, error
    assert_equal 1, JSON.parse(output)['numericReviewFindings'].length
    review['rows'][0]['translation'] = '<b>{0}を3倍</b>'
    File.write(reviewpath, JSON.generate(review))
    output, _, status = cli('validate-translation', '--source', sourcepath, '--input', input, '--numeric-review', reviewpath)
    refute status.success?; assert_includes JSON.parse(output)['mismatches'].map { |r| r['type'] }, 'numbersMismatch'
    # Even an exact reviewed pair cannot authorize changed placeholders/markup.
    translated['entries'][0]['translation'] = '<i>{1}を2倍</i>'
    review['rows'][0]['translation'] = '<i>{1}を2倍</i>'
    File.write(input, JSON.generate(translated)); File.write(reviewpath, JSON.generate(review))
    output, _, status = cli('validate-translation', '--source', sourcepath, '--input', input, '--numeric-review', reviewpath)
    refute status.success?
    types = JSON.parse(output)['mismatches'].map { |r| r['type'] }
    assert_includes types, 'formatMismatch'; assert_includes types, 'markupMismatch'
    review['sourceHash'] = 'stale'; File.write(reviewpath, JSON.generate(review))
    output, _, status = cli('validate-translation', '--source', sourcepath, '--input', input, '--numeric-review', reviewpath)
    refute status.success?; assert_includes JSON.parse(output)['mismatches'].map { |r| r['type'] }, 'numericReviewMetadataMismatch'
  end

  def test_sheet_missing_keys_and_id_mismatch_report_without_mutation
    before = File.read(@shared)
    input = manifest({ 'tabs' => [{ 'title' => 'TownUI', 'rows' => [{ 'key' => 'existing', 'numericIdNote' => '99', 'english' => 'X' }, { 'key' => 'remote', 'numericIdNote' => '23' }] }] })
    output, _, status = cli('sheet-preflight', '--input', input)
    assert status.success?
    types = JSON.parse(output)['differences'].map { |r| r['type'] }
    assert_includes types, 'idMismatch'; assert_includes types, 'sheetOnly'; assert_equal before, File.read(@shared)
    input = manifest({ 'tabs' => [{ 'title' => 'TownUI', 'rows' => [] }] })
    output, _, status = cli('sheet-preflight', '--input', input)
    assert status.success?; assert_equal 'localOnly', JSON.parse(output)['differences'].first['type']
    assert_equal before, File.read(@shared)
  end
end
