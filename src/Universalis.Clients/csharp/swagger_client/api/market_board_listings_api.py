# coding: utf-8

"""
    Universalis

    Welcome to the Universalis documentation page.  <br />  <br />  There is a rate limit of 20 req/s (40 req/s burst) on the API, and 10 req/s (20 req/s burst) on the website itself, if you're scraping instead.  The number of simultaneous connections per IP is capped to 8.    To map item IDs to item names or vice versa, use <a href=\"https://xivapi.com/docs/Search#search\">XIVAPI</a>.  In addition to XIVAPI, you can also get item ID mappings from <a href=\"https://lumina.xiv.dev/docs/intro.html\">Lumina</a>,  <a href=\"https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/Item.csv\">this sheet</a>, or  <a href=\"https://raw.githubusercontent.com/ffxiv-teamcraft/ffxiv-teamcraft/master/apps/client/src/assets/data/items.json\">this</a> pre-made dump.    To get a mapping of world IDs to world names, use <a href=\"https://xivapi.com/World\">XIVAPI</a> or  <a href=\"https://github.com/xivapi/ffxiv-datamining/blob/master/csv/World.csv\">this sheet</a>.  The <code>key</code> column represents the world ID, and the <code>Name</code> column represents the world name.  Note that not all listed worlds are available to be used &#8212; many of the worlds in this sheet are test worlds,  or Korean worlds (Korea is unsupported at this time).    <br />  <br />  If you use this API heavily for your projects, please consider supporting the website on  <a href=\"https://liberapay.com/karashiiro\">Liberapay</a>, <a href=\"https://ko-fi.com/karashiiro\">Ko-fi</a>, or  <a href=\"https://patreon.com/universalis\">Patreon</a>, or making a one-time donation on  <a href=\"https://ko-fi.com/karashiiro\">Ko-fi</a>. Any support is appreciated!    # noqa: E501

    OpenAPI spec version: v2
    
    Generated by: https://github.com/swagger-api/swagger-codegen.git
"""


from __future__ import absolute_import

import re  # noqa: F401

# python 2 and python 3 compatibility library
import six

from swagger_client.api_client import ApiClient


class MarketBoardListingsApi(object):
    """NOTE: This class is auto generated by the swagger code generator program.

    Do not edit the class manually.
    Ref: https://github.com/swagger-api/swagger-codegen
    """

    def __init__(self, api_client=None):
        if api_client is None:
            api_client = ApiClient()
        self.api_client = api_client

    def api_v2_world_or_dc_item_ids_get(self, item_ids, world_or_dc, **kwargs):  # noqa: E501
        """Retrieves the data currently shown on the market board for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once.  # noqa: E501

        This method makes a synchronous HTTP request by default. To make an
        asynchronous HTTP request, please pass async_req=True
        >>> thread = api.api_v2_world_or_dc_item_ids_get(item_ids, world_or_dc, async_req=True)
        >>> result = thread.get()

        :param async_req bool
        :param str item_ids: The item ID or comma-separated item IDs to retrieve data for. (required)
        :param str world_or_dc: The world or data center to retrieve data for. This may be an ID or a name. (required)
        :param str listings: The number of listings to return. By default, all listings will be returned.
        :param str entries: The number of entries to return. By default, a maximum of 5 entries will be returned.
        :param str no_gst: If the result should not have Gil sales tax (GST) factored in. GST is applied to all  consumer purchases in-game, and is separate from the retainer city tax that impacts what sellers receive.  By default, GST is factored in. Set this parameter to true or 1 to prevent this.
        :param str hq: Filter for HQ listings and entries. By default, both HQ and NQ listings and entries will be returned.
        :param str stats_within: The amount of time before now to calculate stats over, in milliseconds. By default, this is 7 days.
        :param str entries_within: The amount of time before now to take entries within, in seconds. Negative values will be ignored.
        :return: CurrentlyShownMultiViewV2
                 If the method is called asynchronously,
                 returns the request thread.
        """
        kwargs['_return_http_data_only'] = True
        if kwargs.get('async_req'):
            return self.api_v2_world_or_dc_item_ids_get_with_http_info(item_ids, world_or_dc, **kwargs)  # noqa: E501
        else:
            (data) = self.api_v2_world_or_dc_item_ids_get_with_http_info(item_ids, world_or_dc, **kwargs)  # noqa: E501
            return data

    def api_v2_world_or_dc_item_ids_get_with_http_info(self, item_ids, world_or_dc, **kwargs):  # noqa: E501
        """Retrieves the data currently shown on the market board for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once.  # noqa: E501

        This method makes a synchronous HTTP request by default. To make an
        asynchronous HTTP request, please pass async_req=True
        >>> thread = api.api_v2_world_or_dc_item_ids_get_with_http_info(item_ids, world_or_dc, async_req=True)
        >>> result = thread.get()

        :param async_req bool
        :param str item_ids: The item ID or comma-separated item IDs to retrieve data for. (required)
        :param str world_or_dc: The world or data center to retrieve data for. This may be an ID or a name. (required)
        :param str listings: The number of listings to return. By default, all listings will be returned.
        :param str entries: The number of entries to return. By default, a maximum of 5 entries will be returned.
        :param str no_gst: If the result should not have Gil sales tax (GST) factored in. GST is applied to all  consumer purchases in-game, and is separate from the retainer city tax that impacts what sellers receive.  By default, GST is factored in. Set this parameter to true or 1 to prevent this.
        :param str hq: Filter for HQ listings and entries. By default, both HQ and NQ listings and entries will be returned.
        :param str stats_within: The amount of time before now to calculate stats over, in milliseconds. By default, this is 7 days.
        :param str entries_within: The amount of time before now to take entries within, in seconds. Negative values will be ignored.
        :return: CurrentlyShownMultiViewV2
                 If the method is called asynchronously,
                 returns the request thread.
        """

        all_params = ['item_ids', 'world_or_dc', 'listings', 'entries', 'no_gst', 'hq', 'stats_within', 'entries_within']  # noqa: E501
        all_params.append('async_req')
        all_params.append('_return_http_data_only')
        all_params.append('_preload_content')
        all_params.append('_request_timeout')

        params = locals()
        for key, val in six.iteritems(params['kwargs']):
            if key not in all_params:
                raise TypeError(
                    "Got an unexpected keyword argument '%s'"
                    " to method api_v2_world_or_dc_item_ids_get" % key
                )
            params[key] = val
        del params['kwargs']
        # verify the required parameter 'item_ids' is set
        if self.api_client.client_side_validation and ('item_ids' not in params or
                                                       params['item_ids'] is None):  # noqa: E501
            raise ValueError("Missing the required parameter `item_ids` when calling `api_v2_world_or_dc_item_ids_get`")  # noqa: E501
        # verify the required parameter 'world_or_dc' is set
        if self.api_client.client_side_validation and ('world_or_dc' not in params or
                                                       params['world_or_dc'] is None):  # noqa: E501
            raise ValueError("Missing the required parameter `world_or_dc` when calling `api_v2_world_or_dc_item_ids_get`")  # noqa: E501

        collection_formats = {}

        path_params = {}
        if 'item_ids' in params:
            path_params['itemIds'] = params['item_ids']  # noqa: E501
        if 'world_or_dc' in params:
            path_params['worldOrDc'] = params['world_or_dc']  # noqa: E501

        query_params = []
        if 'listings' in params:
            query_params.append(('listings', params['listings']))  # noqa: E501
        if 'entries' in params:
            query_params.append(('entries', params['entries']))  # noqa: E501
        if 'no_gst' in params:
            query_params.append(('noGst', params['no_gst']))  # noqa: E501
        if 'hq' in params:
            query_params.append(('hq', params['hq']))  # noqa: E501
        if 'stats_within' in params:
            query_params.append(('statsWithin', params['stats_within']))  # noqa: E501
        if 'entries_within' in params:
            query_params.append(('entriesWithin', params['entries_within']))  # noqa: E501

        header_params = {}

        form_params = []
        local_var_files = {}

        body_params = None
        # HTTP header `Accept`
        header_params['Accept'] = self.api_client.select_header_accept(
            ['text/plain', 'application/json', 'text/json'])  # noqa: E501

        # Authentication setting
        auth_settings = []  # noqa: E501

        return self.api_client.call_api(
            '/api/v2/{worldOrDc}/{itemIds}', 'GET',
            path_params,
            query_params,
            header_params,
            body=body_params,
            post_params=form_params,
            files=local_var_files,
            response_type='CurrentlyShownMultiViewV2',  # noqa: E501
            auth_settings=auth_settings,
            async_req=params.get('async_req'),
            _return_http_data_only=params.get('_return_http_data_only'),
            _preload_content=params.get('_preload_content', True),
            _request_timeout=params.get('_request_timeout'),
            collection_formats=collection_formats)
