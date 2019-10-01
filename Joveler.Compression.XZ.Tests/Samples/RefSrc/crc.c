// https://tukaani.org/xz/xz-file-format.txt
// It was very hard to find any software which could produce CRC64 checksum.
// So when you add new samples to find directory, use this source to get a CRC64.

#include <stddef.h>
#include <inttypes.h>
#include <stdio.h>

uint32_t crc32_table[256];
uint64_t crc64_table[256];

void init(void)
{
	static const uint32_t poly32 = UINT32_C(0xEDB88320);
	static const uint64_t poly64
			= UINT64_C(0xC96C5795D7870F42);

	for (size_t i = 0; i < 256; ++i) {
		uint32_t crc32 = i;
		uint64_t crc64 = i;

		for (size_t j = 0; j < 8; ++j) {
			if (crc32 & 1)
				crc32 = (crc32 >> 1) ^ poly32;
			else
				crc32 >>= 1;

			if (crc64 & 1)
				crc64 = (crc64 >> 1) ^ poly64;
			else
				crc64 >>= 1;
		}

		crc32_table[i] = crc32;
		crc64_table[i] = crc64;
	}
}

uint32_t crc32(const uint8_t *buf, size_t size, uint32_t crc)
{
	crc = ~crc;
	for (size_t i = 0; i < size; ++i)
		crc = crc32_table[buf[i] ^ (crc & 0xFF)]
				^ (crc >> 8);
	return ~crc;
}

uint64_t crc64(const uint8_t *buf, size_t size, uint64_t crc)
{
	crc = ~crc;
	for (size_t i = 0; i < size; ++i)
		crc = crc64_table[buf[i] ^ (crc & 0xFF)]
				^ (crc >> 8);
	return ~crc;
}

int main()
{
	init();

	uint32_t value32 = 0;
	uint64_t value64 = 0;
	uint64_t total_size = 0;
	uint8_t buf[8192];

	while (1) {
		const size_t buf_size
				= fread(buf, 1, sizeof(buf), stdin);
		if (buf_size == 0)
			break;

		total_size += buf_size;
		value32 = crc32(buf, buf_size, value32);
		value64 = crc64(buf, buf_size, value64);
	}

	printf("Bytes:  %" PRIu64 "\n", total_size);
	printf("CRC-32: 0x%08" PRIX32 "\n", value32);
	printf("CRC-64: 0x%016" PRIX64 "\n", value64);

	return 0;
}