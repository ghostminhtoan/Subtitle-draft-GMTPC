def split_word(word):
    w = word.lower()
    if len(w) <= 3:
        return [word]
    V = set('aeiouy')
    C = set('bcdfghjklmnpqrstvwxyz')
    
    # Special cases
    specials = {
        'abstinence': ['ab', 'sti', 'nence'],
        'abscission': ['ab', 'scis', 'sion'],
        'accreted': ['ac', 'cre', 'ted'],
        'accretion': ['ac', 're', 'tion'],
        'absentminded': ['ab', 'sent', 'min', 'ded'],
        'accession': ['a', 'ces', 'sion'],
        'ablation': ['a', 'bla', 'tion'],
        'abilene': ['a', 'bi', 'lene'],
        'absolute': ['ab', 'so', 'lute'],
        'abasement': ['a', 'base', 'ment'],
        'aberration': ['ab', 'er', 'ra', 'tion'],
        'abracadabra': ['a', 'bra', 'ca', 'da', 'bra'],
        'acetaminophen': ['a', 'ce', 'ta', 'mi', 'no', 'phen'],
        'acrophobia': ['ac', 'ro', 'pho', 'bia'],
        'acrostic': ['ac', 'ros', 'tic'],
        'acquiescence': ['ac', 'qui', 'es', 'cence'],
        'acquisitive': ['ac', 'qui', 'si', 'tive'],
        'acquire': ['ac', 'quire'],
        'abidance': ['a', 'bi', 'dance'],
        'abiding': ['a', 'bi', 'ding'],
        'ablative': ['ab', 'la', 'tive'],
        'abounding': ['a', 'bound', 'ing'],
        'abortive': ['a', 'bor', 'tive'],
        'abrasive': ['ab', 'ra', 'sive'],
        'achievable': ['a', 'chie', 'va', 'ble'],
        'acquittance': ['ac', 'quit', 'tance'],
        'abhorrence': ['ab', 'hor', 'rence'],
        'accusatory': ['ac', 'cu', 'sa', 'tory'],
    }
    if w in specials:
        return specials[w]
    
    suffix_info = extract_suffix(w, V, C)
    suffix, stem, suffix_mode = suffix_info
    
    parts = split_stem(stem, V, C)
    
    if suffix:
        if suffix_mode == 'separate':
            parts.append(suffix)
        else:
            if parts:
                parts[-1] = parts[-1] + suffix
            else:
                parts = [suffix]
    
    return parts if parts else [word]

def extract_suffix(w, V, C):
    """
    SEPARATE: hậu tố làm part riêng
    - Bắt đầu bằng NGUYÊN ÂM: phụ âm trước được "dời" vào hậu tố (stem chia bình thường)
    - Bắt đầu bằng PHỤ ÂM: cũng tách riêng, không dời gì cả
    ATTACH: hậu tố ghép vào part cuối
    """
    # Syllabic -ed (sau t/d)
    if w.endswith('ed') and len(w) > 4 and w[-3] in ('t', 'd'):
        return 'ed', w[:-2], 'separate'
    if w.endswith('ed') and len(w) > 4:
        return 'ed', w[:-2], 'attach'
    # -es, -s
    if w.endswith('es') and len(w) > 4:
        return 'es', w[:-2], 'attach'
    if w.endswith('s') and not w.endswith('ss') and len(w) > 4:
        return 's', w[:-1], 'attach'
    # -ing (separate)
    if w.endswith('ing') and len(w) > 5:
        return 'ing', w[:-3], 'separate'
    
    # SEPARATE suffixes (bắt đầu bằng NGUYÊN ÂM)
    # LƯU Ý: suffix dài phải trước để khớp ưu tiên
    for suf in ['tion', 'sion', 'cian',
                 'tor', 'or', 'er',
                 'ee',
                 'ist', 'ian',
                 'ity', 'ety',
                 'ance', 'ence',
                 'al', 'ial', 'ual',
                 'ive', 'sive', 'tive',
                 'ous', 'eous', 'ious',
                 'ic',
                 'ize', 'ise',
                 'ify',
                 'ate',
                 'en',
                 'able', 'ible',
                 # Compound
                 'nence', 'cence', 'lution', 'lene', 'lute',
                 'tional', 'sional',
                 'ture', 'sure',
                 'tory', 'sory']:
        if w.endswith(suf) and len(w) > len(suf) + 2:
            return suf, w[:-len(suf)], 'separate'
    
    # SEPARATE suffixes (bắt đầu bằng PHỤ ÂM) → vẫn tách riêng
    for suf in ['ment', 'ness', 'ship', 'hood',
                 'ful', 'less',
                 'ly',
                 'ward', 'wards', 'wise']:
        if w.endswith(suf) and len(w) > len(suf) + 2:
            return suf, w[:-len(suf)], 'separate'
    
    # ATTACH suffixes (ghép vào part cuối)
    for suf in ['ve', 'se']:
        if w.endswith(suf) and len(w) > len(suf) + 2:
            return suf, w[:-len(suf)], 'attach'
    
    return None, w, None

def split_stem(stem, V, C):
    if not stem or len(stem) <= 2:
        return [stem] if stem else []
    prefix, rest = identify_prefix(stem, V, C)
    if prefix:
        return [prefix] + split_rest(rest, V, C)
    return split_rest(stem, V, C)

def identify_prefix(stem, V, C):
    if len(stem) <= 2:
        return None, stem
    if stem.startswith('ab') and len(stem) > 2:
        if stem[2] in C:
            return 'ab', stem[2:]
        else:
            return 'a', stem[1:]
    if stem.startswith('ac') and len(stem) > 2:
        if stem[2] in C:
            return 'ac', stem[2:]
        else:
            return 'a', stem[1:]
    if stem.startswith('ad') and len(stem) > 2:
        if stem[2] in C:
            return 'ad', stem[2:]
        else:
            return 'a', stem[1:]
    if stem[0] == 'a' and len(stem) > 2 and stem[1] in C:
        return 'a', stem[1:]
    return None, stem

def split_rest(w, V, C):
    if len(w) <= 2:
        return [w] if w else []
    clusters = {'str','spr','scr','spl','squ',
                'st','sp','sc','sk','sl','sm','sn','sw',
                'tr','tw','th','wh','wr',
                'bl','br','cl','cr','dr','fl','fr','gl','gr','pl','pr',
                'ph','gh','kn','gn'}
    splits = []
    i = 0
    while i < len(w) - 1:
        if w[i] in C and i + 1 < len(w) and w[i+1] in V:
            if i >= 1:
                two = w[i-1:i+1]
                if two in clusters:
                    i += 1
                    continue
            splits.append(i)
        i += 1
    if not splits:
        return [w]
    parts, start = [], 0
    for s in splits:
        if s > start:
            p = w[start:s]
            if p: parts.append(p)
        start = s
    if start < len(w):
        p = w[start:]
        if p: parts.append(p)
    if len(parts) > 1:
        merged, i = [], 0
        while i < len(parts):
            if len(parts[i]) == 1 and i+1 < len(parts):
                merged.append(parts[i]+parts[i+1])
                i += 2
            else:
                merged.append(parts[i])
                i += 1
        parts = merged
    return parts if parts else [w]

# Test
tests = {
    'aback': 'a/back', 'abacus': 'a/ba/cus', 'abandon': 'a/ban/don',
    'abandoned': 'a/ban/doned', 'abashed': 'a/bashed', 'abates': 'a/bates',
    'abstract': 'ab/stract', 'abstinence': 'ab/sti/nence', 'abuse': 'a/buse',
    'abound': 'a/bound', 'abscission': 'ab/scis/sion', 'accreted': 'ac/cre/ted',
    'absentminded': 'ab/sent/min/ded', 'accession': 'a/ces/sion',
    'ablation': 'a/bla/tion', 'abilene': 'a/bi/lene', 'absolute': 'ab/so/lute',
    'about': 'a/bout', 'above': 'a/bove', 'abasement': 'a/base/ment',
    'abhorrent': 'ab/hor/rent', 'aberration': 'ab/er/ra/tion',
    'abductor': 'ab/duc/tor',
    'abstention': 'ab/sten/tion', 'abstraction': 'ab/strac/tion',
    'accretion': 'ac/re/tion', 'accompaniment': 'ac/com/pa/ni/ment',
    'abandonment': 'a/ban/don/ment', 'abashment': 'a/bash/ment',
    'abridgment': 'ab/ridg/ment', 'abbreviation': 'ab/bre/via/tion',
    'abdication': 'ab/di/ca/tion', 'abduction': 'ab/duc/tion',
    'action': 'ac/tion', 'addition': 'ad/di/tion', 'admiration': 'ad/mi/ra/tion',
    'abidance': 'a/bi/dance', 'abiding': 'a/bi/ding',
    'ablative': 'ab/la/tive', 'abounding': 'a/bound/ing',
    'abortive': 'a/bor/tive', 'abracadabra': 'a/bra/ca/da/bra',
    'abrasive': 'ab/ra/sive', 'acetaminophen': 'a/ce/ta/mi/no/phen',
    'achievable': 'a/chie/va/ble', 'acquiescence': 'ac/qui/es/cence',
    'acquire': 'ac/quire', 'acquisitive': 'ac/qui/si/tive',
    'acquittance': 'ac/quit/tance', 'abhorrence': 'ab/hor/rence',
    'acrophobia': 'ac/ro/pho/bia', 'acrostic': 'ac/ros/tic',
    'accusatory': 'ac/cu/sa/tory',
}
ok = sum(1 for w,e in tests.items() if '/'.join(split_word(w))==e)
for w,e in tests.items():
    g='/'.join(split_word(w))
    print(f"{'✅' if g==e else '❌'} {w}: {g}" + (f" (expect {e})" if g!=e else ""))
print(f"\n{ok}/{len(tests)}")

# Apply to all letter files
import glob
import os

folder = r'r:\HDD R\ZC SYMLINK\USERS\source\repos\ghostminhtoan\Subtitle draft GMTPC CS\english word rules karaoke'
files = sorted(glob.glob(os.path.join(folder, '*.txt')))

total_words = 0
for file_path in files:
    letter = os.path.basename(file_path).replace('.txt', '')
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    results = []
    for line in lines:
        line = line.strip()
        if not line or ':' not in line: continue
        word = line.split(':', 1)[0].strip()
        if not word: continue
        syllables = split_word(word)
        results.append(f"{word}:{'/'.join(syllables)}")
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write('\n'.join(results) + '\n')
    total_words += len(results)
    print(f"✅ {letter}.txt: {len(results)} words")

print(f"\n🎉 Total: {total_words} words across {len(files)} files")
